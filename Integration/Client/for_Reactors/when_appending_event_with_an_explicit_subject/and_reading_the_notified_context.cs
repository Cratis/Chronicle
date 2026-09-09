// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Reactors;
using context = Cratis.Chronicle.Integration.for_Reactors.when_appending_event_with_an_explicit_subject.and_reading_the_notified_context.context;

namespace Cratis.Chronicle.Integration.for_Reactors.when_appending_event_with_an_explicit_subject;

[Collection(ChronicleCollection.Name)]
public class and_reading_the_notified_context(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : Specification<ChronicleFixture>(chronicleFixture)
    {
        public static readonly TaskCompletionSource<EventContext> Tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public EventSourceId EventSourceId { get; } = $"stream-{Guid.NewGuid()}";
        public Subject Subject { get; } = $"person-{Guid.NewGuid()}";
        public EventContext NotifiedContext { get; private set; } = default!;

        public override IEnumerable<Type> EventTypes => [typeof(SomeEvent)];
        public override IEnumerable<Type> Reactors => [typeof(ReactorCapturingSubject)];

        protected override void ConfigureServices(IServiceCollection services) =>
            services.AddSingleton(new ReactorCapturingSubject(Tcs));

        async Task Because()
        {
            var reactor = EventStore.Reactors.GetHandlerFor<ReactorCapturingSubject>();
            await reactor.WaitTillActive();
            await EventStore.EventLog.Append(EventSourceId, new SomeEvent(42), subject: Subject);
            NotifiedContext = await Tcs.Task.WaitAsync(TimeSpanFactory.DefaultTimeout());
        }
    }

    [Fact] void should_notify_with_the_explicit_subject() => Context.NotifiedContext.Subject.ShouldEqual(Context.Subject);
    [Fact] void should_keep_the_event_source_id_distinct() => Context.NotifiedContext.EventSourceId.ShouldEqual(Context.EventSourceId);
}
