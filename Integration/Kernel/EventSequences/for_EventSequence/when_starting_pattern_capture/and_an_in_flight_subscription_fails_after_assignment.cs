// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Patterns;
using Cratis.Chronicle.Storage;

using context = Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture.and_an_in_flight_subscription_fails_after_assignment.context;
using KernelObserver = Cratis.Chronicle.Observation.IObserver;

namespace Cratis.Chronicle.Kernel.Integration.EventSequences.for_EventSequence.when_starting_pattern_capture;

[Collection(given.PatternCaptureCollection.Name)]
public class and_an_in_flight_subscription_fails_after_assignment(context context) : Given<context>(context)
{
    public class context(given.PatternCaptureFixture fixture) : given.a_log_with_controlled_pattern_capture(fixture)
    {
        public bool ReadinessWaited;
        public bool SubscriptionWasAssigned;
        public Exception? InitialFailure;
        public int InitializationAttempts;
        public bool CompletionSucceeded;

        void Establish()
        {
            _control.Begin(_key);
            _control.FailInitialization = true;
        }

        async Task Because()
        {
            var grains = Services.GetRequiredService<IGrainFactory>();
            var observer = grains.GetGrain<KernelObserver>(new ObserverKey(PatternCapture.ObserverIdentifier, _key.EventStore, _key.Namespace, _key.EventSequenceId));
            var schemas = await Services.GetRequiredService<IStorage>().GetEventStore(_key.EventStore).EventTypes.GetLatestForAllEventTypes();
            var subscription = observer.Subscribe<IPatternCaptureSubscriber>(Concepts.Observation.ObserverType.Reactor, schemas.Select(schema => schema.Type), Services.GetRequiredService<ILocalSiloDetails>().SiloAddress, null, false);
            await _control.InitializationStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));
            try
            {
                SubscriptionWasAssigned = (await observer.GetSubscription()).IsSubscribed;
                var append = await _store.EventLog.Append("customer", new given.CustomerNamed("First"));
                append.IsSuccess.ShouldBeTrue();
                await _control.EnsureRequested.Task.WaitAsync(TimeSpan.FromSeconds(10));
                ReadinessWaited = !_control.SubscriptionCompleted.Task.IsCompleted;
                _control.InitializationReleased.TrySetResult();
                InitialFailure = await Catch.Exception(() => subscription);
                await _control.SubscriptionCompleted.Task.WaitAsync(TimeSpan.FromSeconds(30));

                // Verify recovery before making any further subscription calls: only the timer can rescue it.
                CompletionSucceeded = (await append.WaitForCompletion(TimeSpan.FromSeconds(30))).IsSuccess;
                CompletionSucceeded.ShouldBeTrue();

                // Further ensures coalesce into the completed subscription, rather than restarting it.
                var capture = Services.GetRequiredService<IPatternCapture>();
                await Task.WhenAll(
                    capture.RecoverSubscription(_key.EventStore, _key.Namespace),
                    capture.RecoverSubscription(_key.EventStore, _key.Namespace));
                InitializationAttempts = _control.InitializationAttempts;
            }
            finally
            {
                _control.InitializationReleased.TrySetResult();
            }
        }
    }

    [Fact] void should_observe_the_subscription_during_initialization() => Context.SubscriptionWasAssigned.ShouldBeTrue();
    [Fact] void should_not_report_readiness_before_initialization_finishes() => Context.ReadinessWaited.ShouldBeTrue();
    [Fact] void should_surface_the_original_initialization_failure() => Context.InitialFailure.ShouldNotBeNull();
    [Fact] void should_initialize_once_more_and_then_coalesce_ensures() => Context.InitializationAttempts.ShouldEqual(2);
    [Fact] void should_complete_without_another_append() => Context.CompletionSucceeded.ShouldBeTrue();
}
