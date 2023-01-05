// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Schemas;

namespace Aksio.Cratis.Events.Schemas;

/// <summary>
/// Represents an implementation of <see cref="ISchemas"/>.
/// </summary>
public class Schemas : ISchemas
{
    readonly IEnumerable<EventSchemaDefinition> _definitions;
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schemas"/> class.
    /// </summary>
    /// <param name="eventTypes"><see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for event types.</param>
    public Schemas(
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator)
    {
        _eventTypes = eventTypes;
        _definitions = eventTypes.All.Select(_ =>
        {
            var type = _eventTypes.GetClrTypeFor(_.Id)!;
            return new EventSchemaDefinition(
                _,
                type.Name,
                schemaGenerator.Generate(type));
        });
    }

    /// <inheritdoc/>
    public void RegisterAll()
    {
        throw new NotImplementedException();

        // var schemaStore = _clusterClient.GetGrain<Grains.ISchemaStore>(Guid.Empty);
        // foreach (var schemaDefinition in _definitions)
        // {
        //     schemaStore.Register(
        //         schemaDefinition.Type,
        //         schemaDefinition.FriendlyName,
        //         schemaDefinition.Schema.ToJson());
        // }
    }
}
