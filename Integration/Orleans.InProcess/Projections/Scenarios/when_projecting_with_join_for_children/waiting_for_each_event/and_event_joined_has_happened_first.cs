// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children.waiting_for_each_event.and_event_joined_has_happened_first.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children.waiting_for_each_event;

[Collection(GlobalCollection.Name)]
public class and_event_joined_has_happened_first(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string UserName = "User";

    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<UserProjection, User>(globalFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(GroupCreated), typeof(UserAddedToGroup)];

        void Establish()
        {
            WaitForEachEvent = true;
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            EventSourceId = UserId.ToString();
            ModelId = UserId.ToString();
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_user_name() => Context.Result.Name.ShouldEqual(UserName);
    [Fact] void should_have_group_id_on_child() => Context.Result.Groups.First().GroupId.ShouldEqual(Context.GroupId);
    [Fact] void should_have_group_name_on_child() => Context.Result.Groups.First().GroupName.ShouldEqual(GroupName);
}
