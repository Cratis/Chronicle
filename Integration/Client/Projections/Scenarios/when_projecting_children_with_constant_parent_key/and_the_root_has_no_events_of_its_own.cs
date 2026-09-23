// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Integration.Projections.ProjectionTypes;
using Cratis.Chronicle.Integration.Projections.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_constant_parent_key.and_the_root_has_no_events_of_its_own.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_children_with_constant_parent_key;

[Collection(ChronicleCollection.Name)]
public class and_the_root_has_no_events_of_its_own(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ChildrenWithConstantParentKeyProjection, CountsReadModel>(chronicleFixture)
    {
        public static readonly string StaysOpen = Guid.NewGuid().ToString();
        public static readonly string GetsClosed = Guid.NewGuid().ToString();
        public static readonly string LeavesTheSet = Guid.NewGuid().ToString();

        public override IEnumerable<Type> EventTypes => [typeof(CountedThingRegistered), typeof(CountedThingClosed), typeof(CountedThingRemoved)];

        void Establish()
        {
            ReadModelId = ChildrenWithConstantParentKeyProjection.ConstantKeyValue;

            EventsWithEventSourceIdToAppend.Add(new(StaysOpen, new CountedThingRegistered("stays open", true)));
            EventsWithEventSourceIdToAppend.Add(new(GetsClosed, new CountedThingRegistered("gets closed", true)));
            EventsWithEventSourceIdToAppend.Add(new(LeavesTheSet, new CountedThingRegistered("leaves the set", true)));
            EventsWithEventSourceIdToAppend.Add(new(GetsClosed, new CountedThingClosed()));
            EventsWithEventSourceIdToAppend.Add(new(LeavesTheSet, new CountedThingRemoved()));
        }

        public CountedThing? Thing(string id) => Result.Things.SingleOrDefault(thing => thing.Id == id);
    }

    [Fact] void should_collect_every_event_source_into_one_document() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_the_things_that_did_not_leave() => Context.Result.Things.Count().ShouldEqual(2);
    [Fact] void should_drop_the_thing_that_left() => Context.Thing(context.LeavesTheSet).ShouldBeNull();
    [Fact] void should_carry_the_label_set_from_event_content() => Context.Thing(context.StaysOpen).Label.ShouldEqual("stays open");
    [Fact] void should_leave_the_untouched_thing_open() => Context.Thing(context.StaysOpen).IsOpen.ShouldBeTrue();
    [Fact] void should_apply_the_constant_set_by_the_closing_event() => Context.Thing(context.GetsClosed).IsOpen.ShouldBeFalse();
    [Fact] void should_count_only_the_thing_still_open() => Context.Result.Open.ShouldEqual(1);
}
