// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Concepts.EventSequences.Concurrency;
using Cratis.Chronicle.Concepts.Identities;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_the_constraints_version_check_is_throttled;

/// <summary>
/// A sequence does not only activate in order to append. A read, or an observer subscribing to it, activates it
/// just as well - and it can then sit idle while a client is still registering its artifacts, because constraints
/// are registered after event types are. Counting the activation read as a completed check left the first append
/// validating against the constraints as they were before that registration, which on a store being set up for the
/// first time means against no constraints at all.
/// </summary>
public class and_a_constraint_is_registered_after_activation : given.an_event_sequence
{
    protected override TimeSpan ConstraintsVersionCheckInterval => TimeSpan.FromHours(1);

    void Establish()
    {
        _constraintsGrain.ClearReceivedCalls();
        _registeredConstraints.Add(new UniqueConstraintDefinition("some-constraint", []));
    }

    Task Because() => Append();

    [Fact] void should_pick_the_constraint_up_on_the_first_append() =>
        _constraintsGrain.Received(1).GetDefinitions();

    Task Append() => _eventSequence.Append(
        EventSourceType.Default,
        _eventSourceId,
        EventStreamType.All,
        EventStreamId.Default,
        _eventType,
        new JsonObject(),
        CorrelationId.New(),
        [],
        Identity.System,
        [],
        ConcurrencyScope.None);
}
