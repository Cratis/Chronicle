// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.EventSequences;

using ClientOutcome = Cratis.Chronicle.Observation.ReactorPartitionRetryOutcome;
using ContractOutcome = Cratis.Chronicle.Contracts.Observation.PartitionRecoveryOutcome;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_retrying_a_failed_partition_with_other_outcomes : given.all_dependencies
{
    readonly ContractOutcome[] _outcomes =
    [
        ContractOutcome.PartitionNotFound,
        ContractOutcome.ObserverQuarantined,
        ContractOutcome.PartitionQuarantined,
        (ContractOutcome)999
    ];
    ClientOutcome[] _results;
    int _nextOutcome;

    void Establish()
    {
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
        _observers.RetryPartition(Arg.Any<RetryPartition>()).Returns(_ =>
            new RetryPartitionResponse { Outcome = _outcomes[_nextOutcome++] });
    }

    async Task Because()
    {
        var results = new List<ClientOutcome>();
        foreach (var _ in _outcomes)
        {
            results.Add(await _reactors.RetryFailedPartitionFor<MyReactor>("partition-1"));
        }
        _results = [.. results];
    }

    [Fact] void should_return_partition_not_found() => _results[0].ShouldEqual(ClientOutcome.PartitionNotFound);
    [Fact] void should_return_observer_quarantined() => _results[1].ShouldEqual(ClientOutcome.ObserverQuarantined);
    [Fact] void should_return_partition_quarantined() => _results[2].ShouldEqual(ClientOutcome.PartitionQuarantined);
    [Fact] void should_return_unknown_for_an_unrecognized_outcome() => _results[3].ShouldEqual(ClientOutcome.Unknown);

    [Reactor("73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4")]
    [EventSequence("non-log-sequence")]
    class MyReactor : IReactor;
}
