using System;
using System.Text.Json;

namespace HaKafkaNet;

/// <summary>
/// Provides access to Entities that update in memory automatically. Use in moderation.
/// Entities will perssist in memory until the application exits.
/// Updates lock the entity during update.
/// </summary>
public interface IUpdatingEntityProvider
{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

    IUpdatingEntity<string, JsonElement> GetEntity(string entityId);

    IUpdatingEntity<Tstate, JsonElement> GetEntity<Tstate>(string entityId) where Tstate: class;
    IUpdatingEntity<Tstate, Tatt> GetEntity<Tstate, Tatt>(string entityId)
        where Tstate : class
        where Tatt : class;
    
    IUpdatingEntity<Tstate?, JsonElement> GetValueTypeEntity<Tstate>(string entityId) where Tstate: struct;
    IUpdatingEntity<Tstate?, Tatt> GetValueTypeEntity<Tstate, Tatt>(string entityId)
        where Tstate: struct
        where Tatt : class;
        
    IUpdatingEntity<Tstate, JsonElement> GetEnumEntity<Tstate>(string entityId)
        where Tstate: System.Enum;
    IUpdatingEntity<Tstate, Tatt> GetEnumEntity<Tstate, Tatt>(string entityId)
        where Tstate: System.Enum
        where Tatt : class;   
}


