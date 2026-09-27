// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_registering_fluently.and_handling_typed_and_all_events.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_registering_fluently;

[Collection(ChronicleCollection.Name)]
public class and_handling_typed_and_all_events(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        readonly TaskCompletionSource<(SomeEvent Event, EventContext Context)> _typed = new(TaskCreationOptions.RunContinuationsAsynchronously);
        readonly TaskCompletionSource<object> _catchAllOther = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent), typeof(SomeOtherEvent)];
        public IReactorDefinition Definition;
        public (SomeEvent Event, EventContext Context) Typed;
        public object CatchAllOther;
        public EventSourceId EventSourceId = $"fluent-{Guid.NewGuid()}";

        async Task Because()
        {
            Definition = EventStore.Reactors.Define(
                "fluent-typed-and-all",
                reactor => reactor
                    .On<SomeEvent>(async (@event, eventContext) =>
                    {
                        await Task.Yield();
                        _typed.TrySetResult((@event, eventContext));
                    })
                    .Subscribe((@event, _) =>
                    {
                        if (@event is SomeOtherEvent)
                        {
                            _catchAllOther.TrySetResult(@event);
                        }
                    }));
            var handler = await EventStore.Reactors.Register(Definition);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.AppendMany(EventSourceId, [new SomeEvent(1), new SomeOtherEvent(2)]);
            Typed = await _typed.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
            CatchAllOther = await _catchAllOther.Task.WaitAsync(TimeSpanFactory.FromSeconds(30));
        }

        /// <summary>
        /// The event store outlives this specification, and an all-events reactor left registered would observe every later one.
        /// </summary>
        void Destroy() => EventStore.Reactors.Unregister(Definition.Id);
    }

    [Fact] void should_subscribe_to_all_events() => Context.Definition.SubscribesToAllEvents.ShouldBeTrue();
    [Fact] void should_deliver_the_typed_event() => Context.Typed.Event.Number.ShouldEqual(1);
    [Fact] void should_deliver_the_context_to_the_typed_handler() => Context.Typed.Context.EventSourceId.ShouldEqual(Context.EventSourceId);
    [Fact] void should_deliver_other_events_to_the_catch_all() => ((SomeOtherEvent)Context.CatchAllOther).Number.ShouldEqual(2);
}
