// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture.and_subscription_is_pending.context;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture;

[Collection(given.PatternCaptureCollection.Name)]
public class and_subscription_is_pending(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_log_with_controlled_pattern_capture(fixture)
    {
        public bool AppendSucceeded;
        public bool BatchSucceeded;
        public bool SubscriptionWasStillPending;

        void Establish() => _control.Begin(_key, holdSubscription: true);

        async Task Because()
        {
            var first = await _store.EventLog.Append("customer", new given.CustomerNamed("First"));
            first.IsSuccess.ShouldBeTrue();
            await _control.SubscriptionStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                var later = await _store.EventLog.Append("customer", new given.CustomerNamed("Later")).WaitAsync(TimeSpan.FromSeconds(5));
                AppendSucceeded = later.IsSuccess;
                var batch = await _store.EventLog.AppendMany("customer", [new given.CustomerNamed("Batch")]).WaitAsync(TimeSpan.FromSeconds(5));
                BatchSucceeded = batch.IsSuccess;
                SubscriptionWasStillPending = !_control.SubscriptionReleased.Task.IsCompleted;
            }
            finally
            {
                _control.SubscriptionReleased.TrySetResult();
            }

            await _control.SubscriptionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));
        }
    }

    [Fact] void should_complete_later_appends() => Context.AppendSucceeded.ShouldBeTrue();
    [Fact] void should_complete_later_batches() => Context.BatchSucceeded.ShouldBeTrue();
    [Fact] void should_not_wait_for_subscription_to_finish() => Context.SubscriptionWasStillPending.ShouldBeTrue();
}
