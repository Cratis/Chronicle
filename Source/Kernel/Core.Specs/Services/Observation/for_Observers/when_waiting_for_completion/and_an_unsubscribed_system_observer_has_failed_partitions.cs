// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_unsubscribed_system_observer_has_failed_partitions : given.application_and_system_observers
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerStateStorage.GetAll().Returns([new ObserverState { Identifier = ApplicationObserverId, LastHandledEventSequenceNumber = 0UL }]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions
        {
            Partitions = [new() { ObserverId = SystemObserverId, Partition = Concepts.Keys.Key.Undefined }]
        });
    }

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_include_unsubscribed_system_failures() => _result.FailedPartitions.ShouldBeEmpty();
    [Fact] void should_leave_no_observers_outstanding() => _result.OutstandingObservers.ShouldBeEmpty();
}
