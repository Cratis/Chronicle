// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.for_Observers.when_appending_event;

[Collection(GlobalCollection.Name)]
public class and_waiting_for_observer_to_be_active(and_waiting_for_observer_to_be_active.context fixture) : OrleansTest<and_waiting_for_observer_to_be_active.context>(fixture)
{
    public class context(GlobalFixture globalFixture) : IntegrationTestSetup(globalFixture)
    {
        public static TaskCompletionSource Tsc = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public SomeReaction Reaction;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactions => [typeof(SomeReaction)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reaction = new SomeReaction(Tsc);
            services.AddSingleton(Reaction);
        }

        public override Task Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
            return Task.CompletedTask;
        }
        public override async Task Because()
        {
            await GetObserverFor<SomeReaction>().WaitForState(ObserverRunningState.Active);
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tsc.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }
    }

    [Fact]
    Task should_have_correct_next_sequence_number() => Fixture.ShouldHaveCorrectNextSequenceNumber(1);

    [Fact]
    Task should_have_correct_tail_sequence_number() => Fixture.ShouldHaveCorrectTailSequenceNumber(Concepts.Events.EventSequenceNumber.First);

    [Fact]
    void should_have_handled_the_event() => Fixture.Reaction.HandledEvents.ShouldEqual(1);
}
