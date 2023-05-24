// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Aksio.Boot;
using Aksio.Cratis.Clients;
using Aksio.Cratis.Events;
using Aksio.Schemas;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IPerformBootProcedure"/> for registering event schemas.
/// </summary>
public class SchemasClientLifecycleParticipant : IParticipateInClientLifecycle
{
    readonly IEnumerable<EventTypeRegistration> _definitions;
    readonly IClient _client;
    readonly IEventTypes _eventTypes;
    readonly ILogger<SchemasClientLifecycleParticipant> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schemas"/> class.
    /// </summary>
    /// <param name="client">The Kernel <see cref="IClient"/>.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/>.</param>
    /// <param name="schemaGenerator"><see cref="IJsonSchemaGenerator"/> for generating schemas for event types.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public SchemasClientLifecycleParticipant(
        IClient client,
        IEventTypes eventTypes,
        IJsonSchemaGenerator schemaGenerator,
        ILogger<SchemasClientLifecycleParticipant> logger)
    {
        _client = client;
        _eventTypes = eventTypes;
        _logger = logger;
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
    public async Task ClientConnected()
    {
        _logger.RegisterEventTypes();
        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/types";
        await _client.PerformCommand(route, new RegisterEventTypes(_definitions));
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
