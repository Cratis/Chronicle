// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture.and_the_first_state_snapshot_write_fails.context;
using KernelEventSequenceNumber = Cratis.Chronicle.Concepts.Events.EventSequenceNumber;
using KernelObserver = Cratis.Chronicle.Observation.IObserver;
using KernelObserverId = Cratis.Chronicle.Concepts.Observation.ObserverId;
using KernelObserverKey = Cratis.Chronicle.Concepts.Observation.ObserverKey;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture;

[Collection(given.PatternCaptureCollection.Name)]
public class and_the_first_state_snapshot_write_fails(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_log_with_a_failing_snapshot(fixture)
    {
        public bool AppendSucceeded;
        public bool CompletionSucceeded;
        public bool IsSubscribed;
        public IEnumerable<string> OutstandingObservers;
        public int FailedStateWrites;
        public KernelEventSequenceNumber PersistedSequenceNumber;
        public KernelEventSequenceNumber AppendedSequenceNumber;
        public KernelEventSequenceNumber PatternCaptureLastHandled;
        public KernelEventSequenceNumber EventTypeStatisticsLastHandled;

        async Task Because()
        {
            var append = await _store.EventLog.Append("customer", new given.CustomerNamed("First"));
            AppendSucceeded = append.IsSuccess;
            AppendedSequenceNumber = append.SequenceNumber.Value;
            FailedStateWrites = _snapshotStorage.FailedWrites;
            var storage = Services.GetRequiredService<IStorage>();
            PersistedSequenceNumber = (await storage.GetEventStore(_key.EventStore).GetNamespace(_key.Namespace).GetEventSequence(_key.EventSequenceId).GetState()).SequenceNumber;
            var completion = await append.WaitForCompletion(TimeSpan.FromSeconds(30));
            CompletionSucceeded = completion.IsSuccess;
            OutstandingObservers = completion.OutstandingObservers;
            var grainFactory = Services.GetRequiredService<IGrainFactory>();
            var observer = grainFactory.GetGrain<KernelObserver>(new KernelObserverKey(PatternCapture.ObserverIdentifier, _key.EventStore, _key.Namespace, _key.EventSequenceId));
            IsSubscribed = (await observer.GetSubscription()).IsSubscribed;
            PatternCaptureLastHandled = await WaitForObserverToHandle(PatternCapture.ObserverIdentifier);
            EventTypeStatisticsLastHandled = await WaitForObserverToHandle("$system.statistics.event-types");
        }

        async Task<KernelEventSequenceNumber> WaitForObserverToHandle(KernelObserverId observerId)
        {
            var key = new KernelObserverKey(observerId, _key.EventStore, _key.Namespace, _key.EventSequenceId);
            var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<KernelObserver>(key);
            try
            {
                return await _observerStorage.WaitForHandled(key, AppendedSequenceNumber);
            }
            catch (OperationCanceledException exception)
            {
                var definitions = await Services.GetRequiredService<IStorage>().GetEventStore(_key.EventStore).Observers.GetAll().WaitAsync(TimeSpan.FromSeconds(5));
                var state = await observer.GetState().WaitAsync(TimeSpan.FromSeconds(5));
                var subscription = await observer.GetSubscription().WaitAsync(TimeSpan.FromSeconds(5));
                throw new TimeoutException($"Observer '{key}' did not persist handling {AppendedSequenceNumber}. Registered={definitions.Any(definition => definition.Identifier == observerId)}, LastHandled={state.LastHandledEventSequenceNumber}, RunningState={state.RunningState}, Subscribed={subscription.IsSubscribed}.", exception);
            }
        }
    }

    [Fact] void should_keep_the_durable_append_successful() => Context.AppendSucceeded.ShouldBeTrue();
    [Fact] void should_have_failed_the_snapshot_write() => Context.FailedStateWrites.ShouldEqual(1);
    [Fact] void should_not_require_a_persisted_snapshot() => Context.PersistedSequenceNumber.ShouldEqual(KernelEventSequenceNumber.First);
    [Fact] void should_subscribe_pattern_capture() => Context.IsSubscribed.ShouldBeTrue();
    [Fact] void should_complete_without_another_append() => Context.CompletionSucceeded.ShouldBeTrue();
    [Fact] void should_leave_no_observers_outstanding() => Context.OutstandingObservers.ShouldBeEmpty();
    [Fact] void should_have_pattern_capture_handle_the_appended_event() => Context.PatternCaptureLastHandled.ShouldEqual(Context.AppendedSequenceNumber);
    [Fact] void should_have_event_type_statistics_handle_the_appended_event() => Context.EventTypeStatisticsLastHandled.ShouldEqual(Context.AppendedSequenceNumber);
}
