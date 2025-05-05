// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children.waiting_for_each_event.and_event_joined_happens_after.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children.waiting_for_each_event;

[Collection(ChronicleCollection.Name)]
public class and_event_joined_happens_after(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string UserName = "User";

    public class context(ChronicleFixture ChronicleFixture) : given.a_projection_and_events_appended_to_it<GroupProjection, Group>(ChronicleFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(GroupCreated), typeof(UserAddedToGroup)];

        void Establish()
        {
            WaitForEachEvent = true;
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            EventSourceId = GroupId;
            ModelId = GroupId;

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_group_name() => Context.Result.Name.ShouldEqual(GroupName);
    [Fact] void should_have_user_id_on_child() => Context.Result.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_name_on_child() => Context.Result.Users.First().Name.ShouldEqual(UserName);
}
