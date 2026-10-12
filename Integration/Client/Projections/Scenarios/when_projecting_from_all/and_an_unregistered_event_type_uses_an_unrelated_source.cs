// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_from_all.and_an_unregistered_event_type_uses_an_unrelated_source.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_from_all;

[Collection(ChronicleCollection.Name)]
public class and_an_unregistered_event_type_uses_an_unrelated_source(context context) : Given<context>(context)
{
    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<AllEventsProjection, AllEvents>(chronicleFixture)
    {
        public EventSourceId OpenedId = new(Guid.NewGuid());
        public EventSourceId TouchedId = new(Guid.NewGuid());
        public override IEnumerable<Type> EventTypes => [typeof(ThingOpened), typeof(ThingTouched)];

        void Establish()
        {
            EventSourceId = TouchedId;
            ReadModelId = TouchedId;
            EventsWithEventSourceIdToAppend.Add(new(OpenedId, new ThingOpened("Acme")));
            EventsWithEventSourceIdToAppend.Add(new(TouchedId, new ThingTouched()));
        }

        public Task<AllEvents> Opened() => GetReadModel(OpenedId);
    }

    [Fact] void should_return_the_unrelated_source_by_key() => Context.Result.ShouldNotBeNull();
    [Fact] void should_use_the_unrelated_source_identity() => Context.Result.Id.ToString().ShouldEqual(Context.TouchedId.Value);
    [Fact] void should_count_the_unregistered_event_once() => Context.Result.EventCount.ShouldEqual(1);
    [Fact] async Task should_preserve_the_opened_name() => (await Context.Opened()).Name.ShouldEqual("Acme");
    [Fact] async Task should_count_the_opened_event_once() => (await Context.Opened()).EventCount.ShouldEqual(1);
}
