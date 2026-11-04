// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Contracts.Observation.Reactors;
using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Services.Observation.Reactors.for_Reactors;

public class when_receiving_a_deferred_result : given.an_observation_stream
{
    readonly TaskCompletionSource<ObserverSubscriberResult> _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    ObserverSubscriberResult _result;

    void Establish() => _deliver("partition", [], _completion);

    async Task Because()
    {
        _messages.OnNext(new(new(new ReactorResult
        {
            Partition = "partition",
            DeliveryId = _deliveries[0].DeliveryId,
            State = ObservationState.Deferred,
            LastSuccessfulObservation = 12,
            RetryAfterMilliseconds = 30_000,
            ExceptionStackTrace = string.Empty
        })));
        _result = await _completion.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact] void should_report_the_partition_as_deferred() => _result.State.ShouldEqual(ObserverSubscriberState.Deferred);
    [Fact] void should_wait_as_long_as_the_client_asked() => (_result.Deferral!.Until - _result.Deferral.Since).ShouldEqual(TimeSpan.FromSeconds(30));
    [Fact] void should_resume_after_the_last_handled_event() => _result.Deferral!.ResumeFrom.ShouldEqual((EventSequenceNumber)13UL);
    [Fact] void should_say_the_observer_asked_for_it() => _result.Deferral!.Reason.ShouldEqual(PartitionDeferralReason.RequestedByObserver);
}
