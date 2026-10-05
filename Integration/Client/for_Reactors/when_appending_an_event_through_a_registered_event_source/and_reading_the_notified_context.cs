// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_appending_an_event_through_a_registered_event_source.and_reading_the_notified_context.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_appending_an_event_through_a_registered_event_source;

[Collection(ChronicleCollection.Name)]
public class and_reading_the_notified_context(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public static readonly TaskCompletionSource<EventContext> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EventSourceId EventSourceId { get; } = $"stream-{Guid.NewGuid()}";
        public EventContext NotifiedContext { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> EventSources => [typeof(OrdersEventSource)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorCapturingEventSource)];

        protected override void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new ReactorCapturingEventSource(Tcs));

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorCapturingEventSource>();
            await reactor.WaitTillActive();
            await EventStore.EventLog.AppendThroughEventSource(typeof(OrdersEventSource), EventSourceId, new SomeEvent(42));
            NotifiedContext = await Tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
        }
    }

    [Fact] void should_notify_with_the_event_source_name() => Context.NotifiedContext.EventSource.ShouldEqual(new EventSourceName("Orders"));
}
