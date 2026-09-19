// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventTypes;

/// <summary>
/// Represents the command for registering every event type a client knows about.
/// </summary>
[Command]
[BelongsTo(WellKnownServices.EventTypes)]
public record RegisterEventTypes
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterEventTypes"/> command.
    /// </summary>
    /// <param name="eventStore">The event store to register into.</param>
    /// <param name="types">The event types to register.</param>
    /// <param name="disableValidation">Whether to skip the migration and schema checks, honored in development builds only.</param>
    public RegisterEventTypes(
        EventStoreName eventStore,
        IEnumerable<Contracts.Events.EventTypeRegistration> types,
        bool disableValidation)
    {
        EventStore = eventStore;
        Types = types?.ToList()!;
        DisableValidation = disableValidation;
    }

    /// <summary>
    /// The event store to register into.
    /// </summary>
    public EventStoreName EventStore { get; }

    /// <summary>
    /// The event types to register.
    /// </summary>
    /// <remarks>
    /// A repeated gRPC field may expose a single-use sequence. The command pipeline validates this
    /// property before <see cref="Handle"/> reaches the registrar, so preserving the registrations
    /// here is required for the registrar to see the same values after validation.
    /// </remarks>
    public IEnumerable<Contracts.Events.EventTypeRegistration> Types { get; }

    /// <summary>
    /// Whether to skip the migration and schema checks, honored in development builds only.
    /// </summary>
    public bool DisableValidation { get; }

    /// <summary>
    /// Handles the command by validating the registrations and writing the ones that changed.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event types.</param>
    /// <param name="registrar">The <see cref="EventTypeRegistrar"/> that decides what a registration means.</param>
    /// <param name="eventTypesCacheClient">Client for evicting the event type cache on every silo.</param>
    /// <param name="patternCapture">The <see cref="IPatternCapture"/> to keep observing every registered event type.</param>
    /// <returns>Awaitable task.</returns>
    public Task Handle(
        IStorage storage,
        EventTypeRegistrar registrar,
        IEventTypesCacheClient eventTypesCacheClient,
        IPatternCapture patternCapture)
    {
#if DEVELOPMENT
        var skipValidation = DisableValidation;
#else
        const bool skipValidation = false;
#endif
        return registrar.Register(EventStore, Types, skipValidation, storage, eventTypesCacheClient, patternCapture);
    }
}
