// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Events.EventSequences;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grpc;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents the command for redacting every event of given types for one event source.
/// </summary>
/// <param name="EventStore">The event store the sequence belongs to.</param>
/// <param name="Namespace">The namespace within the event store.</param>
/// <param name="EventSequenceId">The event sequence holding the events.</param>
/// <param name="EventSourceId">The event source whose events are redacted.</param>
/// <param name="Reason">Why the events are being redacted.</param>
/// <param name="EventTypes">The event types to redact. Empty redacts every type for the event source.</param>
/// <param name="Causation">Optional caller-supplied causation chain. Defaults to the request causation when not provided.</param>
/// <param name="CausedBy">Optional caller-supplied identity. Defaults to the current principal when not provided.</param>
[Command]
[BelongsTo(WellKnownServices.EventSequences)]
public record RedactForEventSource(
    string EventStore,
    string Namespace,
    string EventSequenceId,
    string EventSourceId,
    string Reason,
    IEnumerable<string> EventTypes,
    IEnumerable<Causation>? Causation = default,
    Identity? CausedBy = default)
{
    /// <summary>
    /// Handles the command by requesting the redaction of every matching event for the event source.
    /// </summary>
    /// <param name="grainFactory">The <see cref="IGrainFactory"/> to append the redaction request through.</param>
    /// <param name="causation">The <see cref="RequestCausation"/> describing the request behind the append.</param>
    /// <param name="principalAccessor">The <see cref="ICurrentPrincipalAccessor"/> resolving who is redacting.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// A reactor picks up the resulting <see cref="EventsRedactedForEventSource"/> and performs the actual
    /// in-place replacements, rather than the redaction happening synchronously here.
    /// </remarks>
    internal Task Handle(
        IGrainFactory grainFactory,
        RequestCausation causation,
        ICurrentPrincipalAccessor principalAccessor)
    {
        var systemEventSequence = grainFactory.GetSystemEventSequence(EventStore, Namespace);
        return systemEventSequence.Append(
            (EventSourceId)EventSequenceId,
            new EventsRedactedForEventSource(
                EventSequenceId,
                (EventSourceId)EventSourceId,
                EventTypes.Select(eventType => new Concepts.Events.EventType(eventType, 1)),
                Reason),
            correlationId: Guid.NewGuid(),
            causation: Causation?.ToChronicle() ?? causation.GetCurrentChain(),
            causedBy: CausedBy?.ToChronicle() ?? principalAccessor.Current.ToIdentity());
    }
}
