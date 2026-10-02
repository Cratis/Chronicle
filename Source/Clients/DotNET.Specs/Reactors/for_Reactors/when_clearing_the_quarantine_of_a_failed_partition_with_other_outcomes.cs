// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Observation;

using ContractClearOutcome = Cratis.Chronicle.Contracts.Observation.ClearPartitionQuarantineOutcome;
using ContractRecoveryOutcome = Cratis.Chronicle.Contracts.Observation.PartitionRecoveryOutcome;

namespace Cratis.Chronicle.Reactors.for_Reactors;

public class when_clearing_the_quarantine_of_a_failed_partition_with_other_outcomes : given.all_dependencies
{
    readonly ClearPartitionQuarantineResponse[] _responses =
    [
        new() { Outcome = ContractClearOutcome.NotFound },
        new() { Outcome = ContractClearOutcome.NotQuarantined },
        new() { Outcome = ContractClearOutcome.Cleared, RetryOutcome = ContractRecoveryOutcome.ObserverQuarantined },
        new() { Outcome = (ContractClearOutcome)999 }
    ];
    ReactorPartitionQuarantineClearResult[] _results;
    int _nextResponse;
    bool _retryRequested;

    void Establish()
    {
        _eventStore.EventTypes.Returns(_eventTypes);
        _eventTypes.AllClrTypes.Returns([]);
        _reactors.Register<MyReactor>().GetAwaiter().GetResult();
        _observers.ClearPartitionQuarantine(Arg.Any<ClearPartitionQuarantine>()).Returns(call =>
        {
            _retryRequested = call.Arg<ClearPartitionQuarantine>().RetryImmediately;
            return _responses[_nextResponse++];
        });
    }

    async Task Because()
    {
        var results = new List<ReactorPartitionQuarantineClearResult>();
        foreach (var _ in _responses)
        {
            results.Add(await _reactors.ClearFailedPartitionQuarantineFor<MyReactor>("partition-1", false));
        }
        _results = [.. results];
    }

    [Fact] void should_return_not_found() => _results[0].Outcome.ShouldEqual(ReactorPartitionQuarantineClearOutcome.NotFound);
    [Fact] void should_return_not_quarantined() => _results[1].Outcome.ShouldEqual(ReactorPartitionQuarantineClearOutcome.NotQuarantined);
    [Fact] void should_return_the_retry_outcome_when_the_observer_is_quarantined() => _results[2].RetryOutcome.ShouldEqual(ReactorPartitionRetryOutcome.ObserverQuarantined);
    [Fact] void should_return_unknown_for_an_unrecognized_outcome() => _results[3].Outcome.ShouldEqual(ReactorPartitionQuarantineClearOutcome.Unknown);
    [Fact] void should_pass_on_that_no_retry_was_requested() => _retryRequested.ShouldBeFalse();

    [Reactor("73c0c8ed-f2cd-49a2-b5b9-f2f4e1b7b5d4")]
    [EventSequence("non-log-sequence")]
    class MyReactor : IReactor;
}
