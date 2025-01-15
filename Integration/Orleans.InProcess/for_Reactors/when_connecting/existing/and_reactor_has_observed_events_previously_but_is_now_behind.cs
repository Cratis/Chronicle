// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Jobs;
using Cratis.Chronicle.Contracts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Grains.Observation;
using Cratis.Chronicle.Grains.Observation.Jobs;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Storage.Observation;
using Humanizer;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing.and_reactor_has_observed_events_previously_but_is_now_behind.context;
using ObserverRunningState = Cratis.Chronicle.Concepts.Observation.ObserverRunningState;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Reactors.when_connecting.existing;

[Collection(GlobalCollection.Name)]
public class and_reactor_has_observed_events_previously_but_is_now_behind(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_disconnected_reactor_observing_an_event(globalFixture)
    {
        public List<EventForEventSourceId> FirstEvents;
        public List<EventForEventSourceId> CatchupEvents;

        public SomeReactor Reactor;
        public ObserverState ReactorObserverState;

        public EventSequenceNumber LastEventSequenceNumberAfterDisconnect;

        async Task Establish()
        {
            var reactor = await EventStore.Reactors.Register<ReactorWithoutDelay>();
            ReactorObserver = GetObserverFor<ReactorWithoutDelay>();
            await ReactorObserver.WaitTillActive();

            FirstEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            var result = await EventStore.EventLog.AppendMany(FirstEvents);
            var lastHandled = result.SequenceNumbers.Last();

            await ReactorObserver.WaitTillReachesEventSequenceNumber(lastHandled);
            reactor.Disconnect();

            CatchupEvents = EventForEventSourceIdHelpers.CreateMultiple(i => new SomeEvent(42), 10).ToList();
            result = await EventStore.EventLog.AppendMany(CatchupEvents);
            LastEventSequenceNumberAfterDisconnect = result.SequenceNumbers.Last();
        }

        async Task Because()
        {
            await EventStore.Reactors.Register<ReactorWithoutDelay>();
            await ReactorObserver.WaitTillActive();
            ReactorObserverState = await ReactorObserver.GetState();

            await ReactorObserver.WaitTillReachesEventSequenceNumber(LastEventSequenceNumberAfterDisconnect);
            ReactorObserverState = await ReactorObserver.GetState();
        }
    }

    [Fact]
    void should_have_reactor_observer_be_in_running_state() => Context.ReactorObserverState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_catch_up_all_events_added_while_disconnected() => Context.ReactorObserverState.LastHandledEventSequenceNumber.Value.ShouldEqual(Context.LastEventSequenceNumberAfterDisconnect.Value);
}

public static class EventForEventSourceIdHelpers
{
    public static EventForEventSourceId Create(object content, EventSourceId? eventSourceId = null, Auditing.Causation? causation = null)
    {
        return new EventForEventSourceId(
            eventSourceId ?? $"Random event source {Random.Shared.Next()}",
            content,
            causation ?? new Auditing.Causation(DateTimeOffset.UtcNow, Auditing.CausationType.Unknown, new Dictionary<string, string>()));
    }

    public static IEnumerable<EventForEventSourceId> CreateMultiple(Func<int, object> content, int num, EventSourceId? eventSourceId = null)
        => Enumerable.Range(0, num).Select(i => Create(content(i), eventSourceId));
}
