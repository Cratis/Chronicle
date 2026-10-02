// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture.and_a_pending_retry_is_reactivated.context;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture;

[Collection(given.PatternCaptureCollection.Name)]
public class and_a_pending_retry_is_reactivated(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_log_with_controlled_pattern_capture(fixture)
    {
        public bool ActivationChanged;
        public bool CompletionSucceeded;

        void Establish()
        {
            _control.Begin(_key);
            _control.FailSubscription = true;
        }

        async Task Because()
        {
            var append = await _store.EventLog.Append("customer", new given.CustomerNamed("First"));
            append.IsSuccess.ShouldBeTrue();
            await _control.SubscriptionStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            var activation = _control.EventSequenceContext!;
            var grainInterface = activation.GrainInstance!.GetType().GetInterface("IEventSequence")!;
            activation.Deactivate(new DeactivationReason(DeactivationReasonCode.ActivationIdle, "Spec: pending retry activation lost"));
            await activation.Deactivated.WaitAsync(TimeSpan.FromSeconds(10));

            _control.FailSubscription = false;

            // Resolve from the kernel activation: client and kernel IEventSequence have the same full name.
            var grain = Services.GetRequiredService<IGrainFactory>().GetGrain(grainInterface, _key.ToString());
            await ((Task)grainInterface.GetMethod("Rehydrate")!.Invoke(grain, null)!).WaitAsync(TimeSpan.FromSeconds(10));
            await _control.SubscriptionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            ActivationChanged = !ReferenceEquals(activation, _control.EventSequenceContext);
            CompletionSucceeded = (await append.WaitForCompletion(TimeSpan.FromSeconds(30))).IsSuccess;
        }
    }

    [Fact] void should_start_a_new_activation() => Context.ActivationChanged.ShouldBeTrue();
    [Fact] void should_rearm_and_complete_without_another_append() => Context.CompletionSucceeded.ShouldBeTrue();
}
