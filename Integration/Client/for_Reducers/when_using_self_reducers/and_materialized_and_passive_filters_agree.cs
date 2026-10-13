// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Observation;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Reducers;

using context = Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers.and_materialized_and_passive_filters_agree.context;

namespace Cratis.Chronicle.Integration.for_Reducers.when_using_self_reducers;

[Collection(ChronicleCollection.Name)]
public class and_materialized_and_passive_filters_agree(context setup) : Given<context>(setup)
{
    public class context(ChronicleFixture fixture) : Specification<ChronicleFixture>(fixture)
    {
        public SelfLive Live;
        public ClassicLive Classic;
        public SelfPassive Passive;
        public ClassicPassive ClassicOnDemand;
        public int OtherSourceTotal;
        public bool ExcludedKeyAbsent;
        public bool BoundedSelfEmpty;
        public bool BoundedClassicEmpty;
        public override IEnumerable<Type> EventTypes => [typeof(Added)];
        public override IEnumerable<Type> Reducers => [typeof(SelfLive), typeof(SelfPassive), typeof(LiveReducer), typeof(PassiveReducer)];

        async Task Because()
        {
            await EventStore.Reducers.GetHandlerFor(typeof(SelfLive)).WaitTillSubscribed();
            await EventStore.Reducers.GetHandlerFor<LiveReducer>().WaitTillSubscribed();
            await Append("one", 100, "Other", "Payments", "blue");
            await Append("one", 100, "Orders", "Other", "blue");
            await Append("one", 100, "Orders", "Payments", "red");
            await Append("one", 3, "Orders", "Payments", "blue");
            await Append("one", 5, "Orders", "Payments", "green");
            await Append("excluded", 100, "Orders", "Payments", "red");
            await Append("other", 11, "Orders", "Payments", "blue");
            Live = await EventStore.ReadModels.GetInstanceById<SelfLive>("one");
            Classic = await EventStore.ReadModels.GetInstanceById<ClassicLive>("one");
            Passive = await EventStore.ReadModels.GetInstanceById<SelfPassive>("one");
            ClassicOnDemand = await EventStore.ReadModels.GetInstanceById<ClassicPassive>("one");
            OtherSourceTotal = (await EventStore.ReadModels.GetInstanceById<SelfPassive>("other")).Total;
            ExcludedKeyAbsent = await EventStore.ReadModels.GetInstanceById<SelfPassive>("excluded") is null &&
                await EventStore.ReadModels.GetInstanceById<ClassicPassive>("excluded") is null;
            BoundedSelfEmpty = !(await EventStore.ReadModels.GetInstances<SelfPassive>(1)).Any();
            BoundedClassicEmpty = !(await EventStore.ReadModels.GetInstances<ClassicPassive>(1)).Any();
        }

        async Task Append(string source, int amount, string sourceType, string streamType, string tag)
        {
            var append = await EventStore.EventLog.Append(source, new Added(amount), eventStreamType: streamType, eventSourceType: sourceType, tags: [tag]);
            append.IsSuccess.ShouldBeTrue();
            (await append.WaitForCompletion()).IsSuccess.ShouldBeTrue();
        }
    }

    [Fact] void should_apply_tag_or_and_route_category_and_in_the_real_kernel() => Context.Live.Total.ShouldEqual(8);
    [Fact] void should_match_classic_materialized_state() => Context.Classic.Total.ShouldEqual(8);
    [Fact] void should_apply_the_same_stored_metadata_to_passive_self_reads() => Context.Passive.Total.ShouldEqual(Context.Live.Total);
    [Fact] void should_apply_the_same_stored_metadata_to_passive_classic_reads() => Context.ClassicOnDemand.Total.ShouldEqual(Context.Classic.Total);
    [Fact] void should_keep_other_event_sources_separate() => Context.OtherSourceTotal.ShouldEqual(11);
    [Fact] void should_leave_fully_excluded_keys_absent() => Context.ExcludedKeyAbsent.ShouldBeTrue();
    [Fact] void should_take_the_type_selected_prefix_before_metadata_rejection() => (Context.BoundedSelfEmpty && Context.BoundedClassicEmpty).ShouldBeTrue();

    [EventType]
    public record Added(int Amount);
    [Tag("label-only"), FilterEventsByTag("blue"), FilterEventsByTag("green"), EventSourceType("Orders"), EventStreamType("Payments")]
    public record SelfLive(int Total)
    {
        public static SelfLive Reduce(Added e, SelfLive? current) => new((current?.Total ?? 0) + e.Amount);
    }
    [Passive, Tag("label-only"), FilterEventsByTag("blue"), FilterEventsByTag("green"), EventSourceType("Orders"), EventStreamType("Payments")]
    public record SelfPassive(int Total)
    {
        public static SelfPassive Reduce(Added e, SelfPassive? current) => new((current?.Total ?? 0) + e.Amount);
    }
    public record ClassicLive(int Total);
    [Passive]
    public record ClassicPassive(int Total);
    [Tag("label-only"), FilterEventsByTag("blue"), FilterEventsByTag("green"), EventSourceType("Orders"), EventStreamType("Payments")]
    public class LiveReducer : IReducerFor<ClassicLive>
    {
        public ClassicLive Apply(Added e, ClassicLive? current) => new((current?.Total ?? 0) + e.Amount);
    }
    [Tag("label-only"), FilterEventsByTag("blue"), FilterEventsByTag("green"), EventSourceType("Orders"), EventStreamType("Payments")]
    public class PassiveReducer : IReducerFor<ClassicPassive>
    {
        public ClassicPassive Apply(Added e, ClassicPassive? current) => new((current?.Total ?? 0) + e.Amount);
    }
}
