// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the command for registering every event type a client knows about.
/// </summary>
/// <param name="EventStore">The event store to register into.</param>
/// <param name="Types">The event types to register.</param>
/// <param name="DisableValidation">Whether to skip the migration and schema checks, honored in development builds only.</param>
[Command]
[BelongsTo(WellKnownServices.EventTypes)]
public record RegisterEventTypes(
    EventStoreName EventStore,
    IEnumerable<Contracts.Events.EventTypeRegistration> Types,
    bool DisableValidation)
{
    /// <summary>
    /// Handles the command by validating the registrations and writing the ones that changed.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="registrar">The <see cref="EventTypeRegistrar"/> that decides what a registration means.</param>
    /// <param name="eventTypesCacheClient">Client for evicting the event type cache on every silo.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(
        IStorage storage,
        EventTypeRegistrar registrar,
        IEventTypesCacheClient eventTypesCacheClient)
    {
#if DEVELOPMENT
        var skipValidation = DisableValidation;
#else
        const bool skipValidation = false;
#endif
        return registrar.Register(EventStore, Types, skipValidation, storage, eventTypesCacheClient);
    }
}
