// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_replaced_sequence_reports_progress : given.two_clients_with_different_definitions
{
    Task Establish() => RegisterAlternatingClients(ObserverType.Reactor);

    Task Because() => CompleteOldSequence();

    [Fact] void should_leave_the_last_registrants_definition_on_the_first_sequence() => _persistedDefinition.EventSequenceId.ShouldEqual(EventSequenceId.Log);
    [Fact] void should_write_the_replaced_sequences_progress_to_the_shared_cursor() => _persistedState.NextEventSequenceNumber.ShouldEqual((EventSequenceNumber)151UL);
    [Fact] void should_write_the_replaced_sequences_last_handled_position() => _persistedState.LastHandledEventSequenceNumber.ShouldEqual((EventSequenceNumber)150UL);
    [Fact] void should_keep_the_failed_partition_count() => _persistedState.FailedPartitionCount.ShouldEqual((FailedPartitionCount)1);
}
