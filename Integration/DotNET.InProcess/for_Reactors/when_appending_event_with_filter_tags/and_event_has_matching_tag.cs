// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.InProcess.Integration.for_Reactors.when_appending_event_with_filter_tags.and_event_has_matching_tag.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reactors.when_appending_event_with_filter_tags;

[Collection(ChronicleCollection.Name)]
public class and_event_has_matching_tag(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : Specification(chronicleInProcessFixture)
    {
        static readonly TaskCompletionSource _tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public ReactorFilteredByTag Reactor;
        public ReactorState ReactorState;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorFilteredByTag)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reactor = new ReactorFilteredByTag(_tcs);
            services.AddSingleton(Reactor);
        }

        void Establish()
        {
            EventSourceId = "some-source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorFilteredByTag>();
            await reactor.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, Event, tags: ["important"]);
            await _tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
            await reactor.WaitTillReachesEventSequenceNumber(EventSequenceNumber.First);
            ReactorState = await reactor.GetState();
        }
    }

    [Fact] void should_have_handled_the_event() => Context.Reactor.HandledEvents.ShouldEqual(1);

    [Fact]
    void should_have_observer_state_be_active() => Context.ReactorState.RunningState.ShouldEqual(ObserverRunningState.Active);

    [Fact]
    void should_have_correct_last_handled_event_sequence_number() =>
        Context.ReactorState.LastHandledEventSequenceNumber.Value.ShouldEqual(0ul);
}
