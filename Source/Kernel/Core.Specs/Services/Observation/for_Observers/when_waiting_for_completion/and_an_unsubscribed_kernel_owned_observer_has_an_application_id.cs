// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Storage.Observation;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_an_unsubscribed_kernel_owned_observer_has_an_application_id : given.application_and_system_observers
{
    WaitForObserverCompletionResponse _result;

    void Establish()
    {
        _observerDefinitionsStorage.GetAll().Returns(
        [
            new ObserverDefinition(ApplicationObserverId, [new EventType("a-recorded", 1)], Concepts.EventSequences.EventSequenceId.Log, Concepts.Observation.ObserverType.Projection, Concepts.Observation.ObserverOwner.Kernel, false)
        ]);
    }

    async Task Because() => _result = await _observers.WaitForCompletion(_request);

    [Fact] void should_not_complete_successfully() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_time_out_waiting_for_the_projection() => _result.TimedOut.ShouldBeTrue();
    [Fact] void should_report_the_projection_as_outstanding() => _result.OutstandingObservers.ShouldContainOnly(ApplicationObserverId);
    [Fact] void should_not_report_failed_partitions() => _result.FailedPartitions.ShouldBeEmpty();
}
