// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture.and_entering_in_flight_catchup_cannot_be_persisted.context;
using KernelObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture;

[Collection(given.PatternCaptureCollection.Name)]
public class and_entering_in_flight_catchup_cannot_be_persisted(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_log_with_controlled_pattern_capture(fixture)
    {
        public Exception? InitialFailure;
        public Concepts.Observation.ObserverRunningState StalledState;
        public bool CompletionSucceeded;
        public int FailedWrites;
        public ulong LastHandled;

        async Task Establish()
        {
            _control.Begin(_key, holdSubscription: true);
            var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<KernelObserver>(
                new ObserverKey(PatternCapture.ObserverIdentifier, _key.EventStore, _key.Namespace, _key.EventSequenceId));
            await observer.Ensure();
            _control.FailObserverStateWrite = true;
            var schemas = await Services.GetRequiredService<IStorage>().GetEventStore(_key.EventStore).EventTypes.GetLatestForAllEventTypes();
            InitialFailure = await Catch.Exception(() => observer.Subscribe<IPatternCaptureSubscriber>(
                Concepts.Observation.ObserverType.Reactor, schemas.Select(schema => schema.Type), Services.GetRequiredService<ILocalSiloDetails>().SiloAddress, null, false));
            StalledState = (await observer.GetState()).RunningState;
        }

        async Task Because()
        {
            try
            {
                var append = await _store.EventLog.Append("customer", new given.CustomerNamed("First"));
                append.IsSuccess.ShouldBeTrue();
                _control.SubscriptionReleased.TrySetResult();
                await _control.SubscriptionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));
                CompletionSucceeded = (await append.WaitForCompletion(TimeSpan.FromSeconds(30))).IsSuccess;
                var observer = Services.GetRequiredService<IGrainFactory>().GetGrain<KernelObserver>(
                    new ObserverKey(PatternCapture.ObserverIdentifier, _key.EventStore, _key.Namespace, _key.EventSequenceId));
                LastHandled = (await observer.GetState()).LastHandledEventSequenceNumber.Value;
                FailedWrites = _control.FailedObserverStateWrites;
            }
            finally
            {
                _control.SubscriptionReleased.TrySetResult();
            }
        }
    }

    [Fact] void should_surface_the_initial_failure() => Context.InitialFailure.ShouldNotBeNull();
    [Fact] void should_fail_the_entry_write_once() => Context.FailedWrites.ShouldEqual(1);
    [Fact] void should_have_been_stranded_in_in_flight_catchup() => Context.StalledState.ShouldEqual(Concepts.Observation.ObserverRunningState.Unknown);
    [Fact] void should_observe_the_first_event() => Context.LastHandled.ShouldEqual(0UL);
    [Fact] void should_complete_without_another_append() => Context.CompletionSucceeded.ShouldBeTrue();
}
