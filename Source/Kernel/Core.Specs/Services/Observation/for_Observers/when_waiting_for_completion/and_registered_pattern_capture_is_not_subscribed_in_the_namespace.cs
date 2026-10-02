// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Patterns;

namespace Cratis.Chronicle.Services.Observation.for_Observers.when_waiting_for_completion;

public class and_registered_pattern_capture_is_not_subscribed_in_the_namespace : given.a_registered_pattern_capture
{
    WaitForObserverCompletionResponse _result;

    async Task Because() => _result = await _completion.WaitForCompletion(_request);

    [Fact] void should_reconcile_the_stored_owner_to_kernel() => _definitionStorage.State.Owner.ShouldEqual(Concepts.Observation.ObserverOwner.Kernel);
    [Fact] async Task should_register_the_pattern_capture_subscriber() => (await _observer.GetSubscription()).SubscriberType.ShouldEqual(typeof(IPatternCaptureSubscriber));
    [Fact] void should_complete_successfully() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_time_out() => _result.TimedOut.ShouldBeFalse();
    [Fact] void should_leave_no_observers_outstanding() => _result.OutstandingObservers.ShouldBeEmpty();
    [Fact] async Task should_check_the_target_namespace_subscription() => await _unsubscribedObserver.Received(1).GetSubscription();
}
