// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_projection_clients_alternate_event_sequences : given.two_clients_with_different_definitions
{
    Task Because() => RegisterAlternatingClients(ObserverType.Projection);

    [Fact] void should_persist_the_last_registrants_sequence_each_time() => _definitions.Select(_ => _.EventSequenceId).SequenceEqual([EventSequenceId.Log, _secondSequence, EventSequenceId.Log]).ShouldBeTrue();
    [Fact] void should_persist_the_last_registrants_types_each_time() => _definitions.Select(_ => _.EventTypes.Single()).SequenceEqual([_firstType, _secondType, _firstType]).ShouldBeTrue();
    [Fact] void should_keep_the_projection_type_each_time() => _definitions.TrueForAll(_ => _.Type == ObserverType.Projection).ShouldBeTrue();
    [Fact] void should_preserve_the_shared_cursor_each_time() => _states.TrueForAll(_ => _.NextEventSequenceNumber == (EventSequenceNumber)42UL).ShouldBeTrue();
    [Fact] void should_preserve_the_shared_failure_count_each_time() => _states.TrueForAll(_ => _.FailedPartitionCount == (FailedPartitionCount)1).ShouldBeTrue();
    [Fact] void should_start_one_catchup_per_sequence_then_adopt_the_first_job() => _catchupCounts.SequenceEqual([1, 2, 2]).ShouldBeTrue();
    [Fact] void should_leave_both_conflicting_subscriptions_active() => (_firstSubscription.IsSubscribed && _secondSubscription.IsSubscribed).ShouldBeTrue();
}
