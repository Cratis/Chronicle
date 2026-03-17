// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.Concepts;
using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join.waiting_for_each_event.and_event_changes_multiple_properties.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join.waiting_for_each_event;

[Collection(ChronicleCollection.Name)]
public class and_event_changes_multiple_properties(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string UserName = "User";
    const string ProfileName = "ProfileName";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<ProjectionWithJoinOnRoot, User>(chronicleInProcessFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(GroupCreated), typeof(UserAddedToGroup), typeof(UserDetailsChanged)];
        void Establish()
        {
            WaitForEachEvent = true;
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            EventSourceId = UserId.ToString();
            ReadModelId = UserId.ToString();
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated("Some Name")));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserDetailsChanged(UserName, ProfileName)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_user_name() => Context.Result.Name.ShouldEqual(UserName);
    [Fact] void should_have_profile_name() => Context.Result.ProfileName.ShouldEqual(ProfileName);
    [Fact] void should_have_group_id() => Context.Result.GroupId.ShouldEqual(Context.GroupId);
    [Fact] void should_have_group_name() => Context.Result.GroupName.ShouldEqual(GroupName);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
