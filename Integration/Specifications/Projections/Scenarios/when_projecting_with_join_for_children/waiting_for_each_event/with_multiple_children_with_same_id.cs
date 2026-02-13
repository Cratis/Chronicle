// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.Concepts;
using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.ReadModels;
using context = Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_for_children.waiting_for_each_event.with_multiple_children_with_same_id.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_projecting_with_join_for_children.waiting_for_each_event;

[Collection(ChronicleCollection.Name)]
public class with_multiple_children_with_same_id(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string GroupName2 = "Group2";
    const string UserName = "User";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<GroupProjectionWithMultipleJoins, Group>(chronicleInProcessFixture)
    {
        public UserId UserId;
        public EventSourceId FirstGroupId;
        public EventSourceId SecondGroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(SystemUserCreated), typeof(GroupCreated), typeof(UserAddedToGroup), typeof(UserOnboarded), typeof(UserOffboarded), typeof(UserDetailsChanged)];
        public Group FirstGroup;
        public Group SecondGroup;

        void Establish()
        {
            WaitForEachEvent = true;
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            FirstGroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            SecondGroupId = "02cf243d-d8b6-414e-a1e7-d631b656c976";
            EventSourceId = FirstGroupId;
            ReadModelId = FirstGroupId;

            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(SecondGroupId, new GroupCreated(GroupName2)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(SecondGroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserOnboarded()));
        }

        async Task Because()
        {
            FirstGroup = await GetReadModel(FirstGroupId);
            SecondGroup = await GetReadModel(SecondGroupId);
        }
    }

    [Fact] void should_return_first_model() => Context.FirstGroup.ShouldNotBeNull();
    [Fact] void should_return_second_model() => Context.SecondGroup.ShouldNotBeNull();
    [Fact] void should_have_correct_group_name_on_first_group() => Context.FirstGroup.Name.ShouldEqual(GroupName);
    [Fact] void should_have_correct_group_name_on_second_group() => Context.SecondGroup.Name.ShouldEqual(GroupName2);
    [Fact] void should_have_user_id_on_child_on_first_group() => Context.FirstGroup.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_id_on_child_on_second_group() => Context.SecondGroup.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_name_on_child_on_first_group() => Context.FirstGroup.Users.First().Name.ShouldEqual(UserName);
    [Fact] void should_have_user_name_on_child_on_second_group() => Context.SecondGroup.Users.First().Name.ShouldEqual(UserName);
    [Fact] void should_have_user_onboarded_on_child_on_first_group() => Context.FirstGroup.Users.First().Onboarded.ShouldBeTrue();
    [Fact] void should_have_user_onboarded_on_child_on_second_group() => Context.SecondGroup.Users.First().Onboarded.ShouldBeTrue();
}
