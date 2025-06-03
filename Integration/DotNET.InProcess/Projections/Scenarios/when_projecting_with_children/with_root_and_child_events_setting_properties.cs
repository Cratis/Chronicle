// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.AggregateRoots.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.Models;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children.with_root_and_child_events_setting_properties.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_children;

[Collection(ChronicleCollection.Name)]
public class with_root_and_child_events_setting_properties(context context) : Given<context>(context)
{
    const string GroupName = "Group";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<GroupProjection, Group>(chronicleInProcessFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(SystemUserCreated), typeof(GroupCreated), typeof(UserAddedToGroup)];

        void Establish()
        {
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            EventSourceId = GroupId;
            ModelId = GroupId;

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_group_name() => Context.Result.Name.ShouldEqual(GroupName);
    [Fact] void should_have_user_id_on_child() => Context.Result.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
