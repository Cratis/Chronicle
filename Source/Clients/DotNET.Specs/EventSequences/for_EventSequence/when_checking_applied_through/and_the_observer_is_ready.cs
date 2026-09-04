// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using ClientAppliedThroughOutcome = Cratis.Chronicle.Observation.AppliedThroughOutcome;
using ClientAppliedThroughResult = Cratis.Chronicle.Observation.AppliedThroughResult;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_checking_applied_through;

public class and_the_observer_is_ready : given.an_event_sequence
{
    IObservers _observers;
    ClientAppliedThroughResult _result;

    void Establish()
    {
        _observers = Substitute.For<IObservers>();
        services.Observers.Returns(_observers);
        _observers.AppliedThrough(Arg.Any<AppliedThroughRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>()).Returns(new AppliedThroughResponse
        {
            IsSuccess = true,
            Results = [new AppliedThroughObserverResult { ObserverId = "observer-1", Outcome = AppliedThroughOutcome.Ready }]
        });
    }

    async Task Because() => _result = await _eventSequence.AppliedThrough(["observer-1"], 42UL);

    [Fact] void should_be_successful() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_observer_as_ready() => _result.Results.Single().Outcome.ShouldEqual(ClientAppliedThroughOutcome.Ready);
}
