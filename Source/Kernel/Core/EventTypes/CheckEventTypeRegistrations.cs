// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Security;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the command for checking whether event type registrations would be accepted, without registering them.
/// </summary>
/// <remarks>
/// This only reads: nothing is written to the event store, no system event is appended and no cache is evicted.
/// It answers from what is stored, applying the same checks <see cref="RegisterEventTypes"/> applies.
/// </remarks>
[StoreAccess(StoreAccess.Read)]
[Command]
[BelongsTo(WellKnownServices.EventTypes)]
public record CheckEventTypeRegistrations
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CheckEventTypeRegistrations"/> command.
    /// </summary>
    /// <param name="eventStore">The event store to check against.</param>
    /// <param name="types">The event type registrations to check.</param>
    public CheckEventTypeRegistrations(EventStoreName eventStore, IEnumerable<Contracts.Events.EventTypeRegistration> types)
    {
        EventStore = eventStore;
        Types = types?.ToList()!;
    }

    /// <summary>
    /// The event store to check against.
    /// </summary>
    public EventStoreName EventStore { get; }

    /// <summary>
    /// The event type registrations to check.
    /// </summary>
    /// <remarks>
    /// Preserved as a list because a repeated gRPC field may expose a single-use sequence that validation would consume.
    /// </remarks>
    public IEnumerable<Contracts.Events.EventTypeRegistration> Types { get; }

    /// <summary>
    /// Handles the command by checking each registration against what is stored.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="registrar">The <see cref="EventTypeRegistrar"/> that decides what a registration means.</param>
    /// <returns>One result per registration.</returns>
    public async Task<CheckEventTypeRegistrationsResult> Handle(IStorage storage, EventTypeRegistrar registrar) =>
        new(await registrar.Check(EventStore, Types, storage));
}
