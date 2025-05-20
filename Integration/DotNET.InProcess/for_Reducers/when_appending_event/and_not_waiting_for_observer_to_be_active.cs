// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.InProcess.Integration.for_Reducers.when_appending_event.and_not_waiting_for_observer_to_be_active.context;

namespace Cratis.Chronicle.InProcess.Integration.for_Reducers.when_appending_event;

[Collection(ChronicleCollection.Name)]
public class and_not_waiting_for_observer_to_be_active(context context) : Given<context>(context)
{
    public class context(ChronicleFixture ChronicleFixture) : IntegrationSpecificationContext(ChronicleFixture)
    {
        public static TaskCompletionSource Tsc = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public EventSourceId EventSourceId;
        public SomeEvent Event;
        public SomeReducer Reducer;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reducers => [typeof(SomeReducer)];

        protected override void ConfigureServices(IServiceCollection services)
        {
            Reducer = new SomeReducer(Tsc);
            services.AddSingleton(Reducer);
        }

        void Establish()
        {
            EventSourceId = "some source";
            Event = new SomeEvent(42);
        }

        async Task Because()
        {
            await EventStore.EventLog.Append(EventSourceId, Event);
            await Tsc.Task.WaitAsync(TimeSpan.FromMilliseconds(10));
        }
    }

#pragma warning disable xUnit1004
    [Fact(Skip = "Not waiting for the observer does not work")]
#pragma warning restore xUnit1004
    Task should_have_correct_next_sequence_number() => Context.ShouldHaveNextSequenceNumber(1);

#pragma warning disable xUnit1004
    [Fact(Skip = "Not waiting for the observer does not work")]
#pragma warning restore xUnit1004
    Task should_have_correct_tail_sequence_number() => Context.ShouldHaveTailSequenceNumber(EventSequenceNumber.First);

#pragma warning disable xUnit1004
    [Fact(Skip = "Not waiting for the observer does not work")]
#pragma warning restore xUnit1004
    void should_have_handled_the_event() => Context.Reducer.HandledEvents.ShouldEqual(1);
}
