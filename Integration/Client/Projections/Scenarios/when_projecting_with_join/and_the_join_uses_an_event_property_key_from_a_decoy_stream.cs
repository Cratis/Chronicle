// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Events;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join.and_the_join_uses_an_event_property_key_from_a_decoy_stream.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.when_projecting_with_join;

[Collection(ChronicleCollection.Name)]
public class and_the_join_uses_an_event_property_key_from_a_decoy_stream(context context) : Given<context>(context)
{
    const string GroupName = "Keyed group";

    public class context(ChronicleFixture chronicleFixture) : given.a_projection_and_events_appended_to_it<ProjectionWithKeyedJoinOnRoot, User>(chronicleFixture)
    {
        const string UserId = "3c760aaf-2119-4336-8721-3f4c97e86a1b";
        const string DecoyUserId = "564ac42d-66ba-444f-b265-fd42e900df75";
        const string GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
        const string DecoyStreamId = "b1fe04bc-2f62-4e2b-93b9-6a26a80cdb83";
        public User Decoy;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(UserAddedToGroup), typeof(GroupNamed)];

        void Establish()
        {
            EventSourceId = UserId;
            ReadModelId = UserId;
            WaitForEachEvent = true;
            EventsWithEventSourceIdToAppend.Add(new(UserId, new UserCreated("User")));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(DecoyUserId, new UserCreated("Decoy")));
            EventsWithEventSourceIdToAppend.Add(new(DecoyStreamId, new UserAddedToGroup(DecoyUserId)));

            // Roots must exist first: deferred join backfill still looks up the joined stream by event source id.
            EventsWithEventSourceIdToAppend.Add(new(DecoyStreamId, new GroupNamed(GroupId, GroupName)));
        }

        protected override async Task<User> GetReadModelResult()
        {
            Decoy = await GetReadModel(DecoyUserId);
            return await base.GetReadModelResult();
        }
    }

    [Fact] void should_name_the_keyed_group() => Context.Result.GroupName.ShouldEqual(GroupName);
    [Fact] void should_not_name_the_decoy_group() => string.IsNullOrEmpty(Context.Decoy.GroupName).ShouldBeTrue();
    [Fact] void should_record_the_join_on_the_target() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
    [Fact] void should_leave_the_decoy_unchanged() => Context.Decoy.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber - 1);
}
