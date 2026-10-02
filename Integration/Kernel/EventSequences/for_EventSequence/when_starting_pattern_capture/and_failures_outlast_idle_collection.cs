// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture.and_failures_outlast_idle_collection.context;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture;

[Collection(given.PatternCaptureCollection.Name)]
public class and_failures_outlast_idle_collection(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_log_with_controlled_pattern_capture(fixture)
    {
        public bool ActivationSurvived;
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

            // Four timer attempts span 15 seconds, beyond the fixture's 10-second collection age.
            // No calls reach the event log while its dependencies fail before observer subscription.
            await _control.RetriedBeyondCollectionAge.Task.WaitAsync(TimeSpan.FromSeconds(30));
            ActivationSurvived = !activation.Deactivated.IsCompleted;
            _control.FailSubscription = false;
            await _control.SubscriptionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));
            CompletionSucceeded = (await append.WaitForCompletion(TimeSpan.FromSeconds(30))).IsSuccess;
        }
    }

    [Fact] void should_keep_the_pending_retry_alive() => Context.ActivationSurvived.ShouldBeTrue();
    [Fact] void should_recover_on_a_later_tick_without_another_append() => Context.CompletionSucceeded.ShouldBeTrue();
}
