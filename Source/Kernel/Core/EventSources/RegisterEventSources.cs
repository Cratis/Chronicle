// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.EventSources;

/// <summary>
/// Represents the command for registering every event source definition a client knows about.
/// </summary>
/// <remarks>
/// Registering a definition that is already registered replaces it. Definitions that are no longer registered are
/// kept, because stored events refer to them.
/// </remarks>
[Command]
[BelongsTo(WellKnownServices.EventSources)]
public record RegisterEventSources
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterEventSources"/> command.
    /// </summary>
    /// <param name="eventStore">The event store to register into.</param>
    /// <param name="sources">The event source definitions to register.</param>
    public RegisterEventSources(
        EventStoreName eventStore,
        IEnumerable<Contracts.EventSources.EventSourceDefinition> sources)
    {
        EventStore = eventStore;
        Sources = sources?.ToList()!;
    }

    /// <summary>
    /// The event store to register into.
    /// </summary>
    public EventStoreName EventStore { get; }

    /// <summary>
    /// The event source definitions to register.
    /// </summary>
    /// <remarks>
    /// A repeated gRPC field may expose a single-use sequence. The command pipeline validates this
    /// property before <see cref="Handle"/> runs, so the registrations are preserved here.
    /// </remarks>
    public IEnumerable<Contracts.EventSources.EventSourceDefinition> Sources { get; }

    /// <summary>
    /// Handles the command by saving the definitions.
    /// </summary>
    /// <param name="storage">The <see cref="IStorage"/> holding the event source definitions.</param>
    /// <returns>Awaitable task.</returns>
    public async Task Handle(IStorage storage)
    {
        var eventSources = storage.GetEventStore(EventStore).EventSources;
        foreach (var source in Sources)
        {
            await eventSources.Save(source.ToConcept());
        }
    }
}
