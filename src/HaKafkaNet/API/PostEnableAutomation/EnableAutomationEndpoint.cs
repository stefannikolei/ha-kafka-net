﻿#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

using FastEndpoints;
using Microsoft.Extensions.Logging;

namespace HaKafkaNet;

internal class EnableAutomationEndpoint : Endpoint<EnableEndpointRequest>
{
    private readonly IAutomationManager _automationManager;
    private readonly ILogger<EnableAutomationEndpoint> _logger;

    public EnableAutomationEndpoint(IAutomationManager automationManager, ILogger<EnableAutomationEndpoint> logger)
    {
        this._automationManager = automationManager;
        this._logger = logger;
    }

    public override void Configure()
    {
        Post("/api/automation/enable");
        AllowAnonymous();
    }

    public override Task HandleAsync(EnableEndpointRequest req, CancellationToken ct)
    {
        if (_automationManager.EnableAutomation(req.Key, req.Enable))
        {
            return SendOkAsync(ct);
        }
        return SendNotFoundAsync(ct);
    }
}

public class EnableEndpointRequest
{
    //public Guid Id { get; set; }
    public required string Key { get; set; }
    public bool Enable { get; set; }
}
