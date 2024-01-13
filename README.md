# ha-kafka-net
Integration that uses Home Assistant Kafka integration for creating home automations in .NET
It was created with the following goals:
* Create Home Assistant automations in .NET
* Expose a simple way to track states of all entities in Home Asstant
* Expose a simple way to respond to Home Assistant state changes
* Provide a means to call Home Assistant RESTful services
* Enable all automation code to be fully unit testable

This project is still in an alpha state. 
Nuget package can be found [here](https://www.nuget.org/packages/HaKafkaNet/).

## Why ha-kafka-net ?
* Kafka allows you to replay events. Therefore, when your application starts, it can quickly load the states of all your Home Assistant entities.
* It gives you a easy-to-spin up infrastructure with management features out of the box. This includes docker images for both managing kafka and seeing the state of your consumers based on open source projects
* You have an easy way to respond to events during start up which means you are guarenteed to see/handle all events at least once, even if your application has been down.
* Full unit testability
* MIT license

## How it works
* Events are streamed from Home Assistant to the `home_assistant` topic. Unfortunately, the key is not utilizied by the provided home assistant kafka integration. Please upvote [this feature request](https://community.home-assistant.io/t/set-key-in-kafka-topic/671757/2)
* The transformer reads the messages and then adds them to the `home_assistant_states` topic with the entity id set as a key.
  - This allows us to compact the topic and make some assurances about order.
* A second consumer called the state handler reads from `home_assistant_states` topic and caches all state changes exposed by home assistant to Redis.
  - This allows for faster retrieval later and minimizes our application memory footprint. It also allows us to have some knowledge about which events were not handled between restarts and which ones were. The framework will tell your automation about such timings to allow you to handle messages appropriately.
* It then looks for automations which want to be notified.
  - If the entity id of the state change matches any of the `TriggerEntityIds` exposed by your automation, and the timing of the event matches your specified timings, then the `Execute` method of your automation will be called with a new `Task`.
  - It is up to the consumer to handle any errors. The framework prioritizes handling new messages speedily over tracking the state of individual automations. If your automation erros it will only write an ILogger message indicating the error.

## Current steps for Example App set up:
1. Edit the `~/infrastructure/docker-compose.yml` for your environment and run it.
   - you  need to edit the values for the external listner of `KAFKA_CFG_ADVERTISED_LISTENERS` and persistent storage.
2. Connect to your kafka-ui instance on port 8080 and create two topics.
   - home_assistant - This is where Home Assistant will deposit state changes. Set the retention to your liking.
   - home_assistant_states - This is where the transformer will add keys to the messages based on the Home Assistant EntityId. Set the clean up policy to `compact`. Optionally add a custom parameter named `max.compaction.lag.ms` to force compaction to run more often which should save space and prevent excessively handling of old messages at startup.
3. Launch your Home Assistant UI and edit your `configuration.yaml` to include kafka.
   - see [Apache Kafka integration documentation](https://www.home-assistant.io/integrations/apache_kafka/)
   - set the topic to `home_assistant`
   - set the port to `9094` if not running on the same machine.
   - It is recommended to set an `include_domains` filter, otherwise you will produce hundreds of thousands of events every week. You should include all domains for all entities that you plan to respond to or inspect in your automaions. For the included examples to run, you should, at a minimum, include:
     - `light`
     - `input_button`
4. Restart Home Assistant
   - At this point events should be streaming from Home Assistant into the `home_assistant` topic, which you can inspect via your kafka-ui instance.
5. In the `~/example/HakafkaNet.ExampleApp` directory, create an `appsettings.Development.json` file.
   - Copy/paste the contents of the `appsettings.json` file and modify appropriately.
   - You will need a long lived access token for Home Assistant, which you can get from Home Assitant UI or follow directions here: [Long Lived Access Tokens](https://developers.home-assistant.io/docs/auth_api/#long-lived-access-token) 

At this point your environment is set up and ready for development. If you run the example app, you can watch the consumers via a dashboard provided at  `localhost:<port>/kafkaflow`. It is provided via [KafkaFlow](https://github.com/Farfetch/kafkaflow). You could also connect to redis to see events being cached. To see the example automations in action, continue with these steps:
1. In your HomeAssistant UI, create two helper buttons named:
   - `Test Button`
   - `Test Button 2`
2. Modify the `example/HaKafkaNet.ExampleApp/Automations/SimpleLightAutomation.cs` file and set `_idOfLightToDim` to an id of a light that exists in your Home Assistant instance
3. Click your test buttons both while your application is up and while it is down to see different behaviors at starup.

## Setup with nuget package
1. Follow the infrastructure instructions above for Kafka and Redis
2. Create a new web app: `dotnet new web`
3. Add HaKafkaNet: `dotnet add package HaKafkaNet`
4. Add an `IDistributedCache` implementation of your choosing. For example: `dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis`
5. Copy the `appsettings.json` file from the `example/HaKafkaNet.ExampleApp` directory into your project and modify.
6. Add the following code to your `program.cs`
```
using HaKafkaNet;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

HaKafkaNetConfig config = new HaKafkaNetConfig();
builder.Configuration.GetSection("HaKafkaNet").Bind(config);

// provide an IDistributedCache implementation
services.AddStackExchangeRedisCache(options => 
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConStr");
});

services.AddHaKafkaNet(config);

// add your own services as needed

var app = builder.Build();

await app.StartHaKafkaNet(config);

app.MapGet("/", () => "HaKafkaNet is running");

app.Run();
```
7. Create your automations by implementing the `IAutomation` interface.

## Tips
* You can optionally add this repository as a submodule to your own instead of using the nuget package.
* During start up, it can take a minute or two for it to churn though thousands of events. In the output, you can see which kafka offsets have been handled. You can then compare that to the current offset which you can discover from your kafka-ui instance
* ILogger support has been added. When your automation is called, the name of your automation, the entity id of the entity change that triggered it, and the context id from Home Assistant will be added to the scope.
* You can run the transformer seperately from the state manager and your automations. This allows you to constantly have the transformers work up to date if your automations are shut down for development or other reasons.

## Features added
* Some common API calls
* Contextual Logging

## TODO:
* More automated tests
* Enhanced API functionality
