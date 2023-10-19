// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Connections;
using Aksio.Cratis.Events;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis.Schemas;

/// <summary>
/// Represents an implementation of <see cref="IParticipateInConnectionLifecycle"/> for registering event schemas.
/// </summary>
public class SchemasConnectionLifecycleParticipant : IParticipateInConnectionLifecycle
{
    readonly IEnumerable<EventTypeRegistration> _definitions;
    readonly IConnection _connection;
    readonly ILogger<SchemasConnectionLifecycleParticipant> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="Schemas"/> class.
    /// </summary>
    /// <param name="connection">The Kernel <see cref="IConnection"/>.</param>
    /// <param name="eventTypes"><see cref="IEventTypes"/>.</param>
    /// <param name="logger"><see cref="ILogger"/> for logging.</param>
    public SchemasConnectionLifecycleParticipant(
        IConnection connection,
        IEventTypes eventTypes,
        ILogger<SchemasConnectionLifecycleParticipant> logger)
    {
        _connection = connection;
        _logger = logger;
        _definitions = eventTypes.AllAsRegistrations;
    }

    /// <inheritdoc/>
    public async Task ClientConnected()
    {
        _logger.RegisterEventTypes();
        var route = $"/api/events/store/{ExecutionContextManager.GlobalMicroserviceId}/types";
        await _connection.PerformCommand(route, new RegisterEventTypes(_definitions));
    }

    /// <inheritdoc/>
    public Task ClientDisconnected() => Task.CompletedTask;
}
