// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Cratis.Boot;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Cratis.Execution;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IPerformBootProcedure"/> for registering event schemas.
/// </summary>
public class SchemasBootProcedure : IParticipateInClientLifecycle
{
    readonly IEnumerable<EventTypeRegistration> _definitions;
    readonly IClient _client;
    readonly IEventTypes _eventTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schemas"/> class.
    /// </summary>
    /// <param name="client"></param>
    /// <param name="eventTypes"><see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for event types.</param>
    public SchemasBootProcedure(
        IClient client,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator)
    {
        _client = client;
        _eventTypes = eventTypes;
        _definitions = eventTypes.All.Select(_ =>
        {
            var type = _eventTypes.GetClrTypeFor(_.Id)!;
            return new EventTypeRegistration(
                _,
                type.Name,
                JsonNode.Parse(schemaGenerator.Generate(type).ToJson())!);
        }).ToArray();
    }

    /// <inheritdoc/>
    public async Task Connected()
    {
        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/types";
        await _client.PerformCommand(route, new RegisterEventTypes(_definitions));
    }

    /// <inheritdoc/>
    public Task Disconnected() => Task.CompletedTask;
}
