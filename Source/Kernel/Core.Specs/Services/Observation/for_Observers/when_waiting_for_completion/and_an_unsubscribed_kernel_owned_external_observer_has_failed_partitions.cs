// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_unsubscribed_kernel_owned_external_observer_has_failed_partitions : given.application_and_system_observers
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(ApplicationObserverId, [new EventType("a-recorded", 1)], Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.External, Concepts.Observation.ObserverOwner.Kernel, false)
        ]);
        _failedPartitionsStorage.GetFor(Arg.Any<IEnumerable<ObserverId>>()).Returns(new Concepts.Observation.FailedPartitions
        {
            Partitions = [new() { ObserverId = ApplicationObserverId, Partition = Concepts.Keys.Key.Undefined }]
        });
    }

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_not_complete_successfully() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_external_observer_failure() => _result.FailedPartitions.Single().ObserverId.ShouldEqual(ApplicationObserverId);
    [Fact] void should_not_time_out() => _result.TimedOut.ShouldBeFalse();
}
