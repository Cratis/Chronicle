// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers.and_external_events_arrive_through_the_inferred_inbox.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers;

[Collection(ChronicleCollection.Name)]
public class and_external_events_arrive_through_the_inferred_inbox(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public SelfTotal Self;
        public ClassicTotal ClassicState;
        public string[] SubscriptionEvents;
        public string Sequence;
        public override IEnumerable<Type> EventTypes => [typeof(ExternalAdded)];
        public override IEnumerable<Type> Reducers => [typeof(SelfTotal), typeof(Classic)];

        async Task Because()
        {
            var producer = await ChronicleClient.GetEventStore("4731-source", EventStore.Namespace);
            await producer.RegisterAll();
            producer.Registration.IsSuccess.ShouldBeTrue();
            var consumer = await ChronicleClient.GetEventStore("4731-consumer", EventStore.Namespace);
            await consumer.RegisterAll();
            consumer.Registration.IsSuccess.ShouldBeTrue();
            var handlers = new[] { consumer.Reducers.GetHandlerFor(typeof(SelfTotal)), consumer.Reducers.GetHandlerFor<Classic>() };
            foreach (var handler in handlers) await handler.WaitTillSubscribed();
            var source = EventSourceId.New();
            var outbox = producer.GetEventSequence(EventSequenceId.Outbox);

            (await outbox.Append(source, new ExternalAdded(100), eventStreamType: "Other", eventSourceType: "Orders", tags: ["important"])).IsSuccess.ShouldBeTrue();
            (await outbox.Append(source, new ExternalAdded(100), eventStreamType: "Payments", eventSourceType: "Orders", tags: ["ignored"])).IsSuccess.ShouldBeTrue();
            var append = await outbox.Append(source, new ExternalAdded(7), eventStreamType: "Payments", eventSourceType: "Orders", tags: ["important", "blue"]);
            append.IsSuccess.ShouldBeTrue();
            foreach (var handler in handlers) await handler.WaitTillReachesEventSequenceNumber(append.SequenceNumber);
            Self = await consumer.ReadModels.GetInstanceById<SelfTotal>(source);
            ClassicState = await consumer.ReadModels.GetInstanceById<ClassicTotal>(source);
            Sequence = handlers[0].EventSequenceId.Value;
            SubscriptionEvents = (await consumer.Subscriptions.GetAll()).Single().EventTypes.Select(type => type.Value).ToArray();
        }
    }

    [Fact] void should_infer_the_foreign_inbox_sequence() => Context.Sequence.ShouldEqual("inbox-4731-source");
    [Fact] void should_union_and_deduplicate_the_actual_dispatch_events() => Context.SubscriptionEvents.ShouldEqual<string>(["4731-external-added"]);
    [Fact] void should_release_the_foreign_event_into_the_self_model() => Context.Self.Total.ShouldEqual(7);
    [Fact] void should_preserve_the_foreign_route_and_tag_filters_for_both_forms() => Context.ClassicState.Total.ShouldEqual(Context.Self.Total);

    [EventType("4731-external-added"), EventStore("4731-source")]
    public record ExternalAdded(int Amount);
    [EventSourceType("Orders"), EventStreamType("Payments"), FilterEventsByTag("important")]
    public record SelfTotal(int Total)
    {
        public static SelfTotal Reduce(ExternalAdded e, SelfTotal? current) => new((current?.Total ?? 0) + e.Amount);
        public static string Helper(string value) => value;
    }
    public record ClassicTotal(int Total);
    [EventSourceType("Orders"), EventStreamType("Payments"), FilterEventsByTag("important")]
    public class Classic : IReducerFor<ClassicTotal>
    {
        public ClassicTotal Apply(ExternalAdded e, ClassicTotal? current) => new((current?.Total ?? 0) + e.Amount);
    }
}
