// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

using ClientOutcome = Cratis.Chronicle.Observation.ReactorPartitionRetryOutcome;
using ContractOutcome = Cratis.Chronicle.Contracts.Observation.PartitionRecoveryOutcome;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_retrying_a_failed_partition : given.all_dependencies
{
    readonly ReactorId _reactorId = "73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4";
    readonly EventSequenceId _eventSequenceId = "non-log-sequence";
    readonly Partition _partition = "partition-1";
    ClientOutcome _result;

    void Establish()
    {
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
        _observers.RetryPartition(Arg.Any<RetryPartition>()).Returns(new RetryPartitionResponse { Outcome = ContractOutcome.Started });
    }

    async Task Because()
    {
        IReactors reactors = _reactors;
        _result = await reactors.RetryFailedPartitionFor<MyReactor>(_partition);
    }

    [Fact] void should_return_started() => _result.ShouldEqual(ClientOutcome.Started);
    [Fact] void should_target_the_handler_event_sequence_and_partition() =>
        _observers.Received(1).RetryPartition(Arg.Is<RetryPartition>(request =>
            request.EventStore == _eventStore.Name.Value &&
            request.Namespace == _eventStore.Namespace.Value &&
            request.ObserverId == _reactorId.Value &&
            request.EventSequenceId == _eventSequenceId.Value &&
            request.Partition == _partition.Value));

    [Reactor("73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4")]
    [EventSequence("non-log-sequence")]
    class MyReactor : IReactor;
}
