// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ReadModels;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_join_for_children.with_multiple_children_with_same_id.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_join_for_children;

[Collection(ChronicleCollection.Name)]
public class with_multiple_children_with_same_id(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string GroupName2 = "Group2";
    const string UserName = "User";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<GroupProjectionWithMultipleJoins, Group>(chronicleInProcessFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public EventSourceId GroupId2;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(SystemUserCreated), typeof(GroupCreated), typeof(UserAddedToGroup), typeof(UserOnboarded), typeof(UserOffboarded), typeof(UserDetailsChanged)];
        public Group ReadModel1;
        public Group ReadModel2;
        void Establish()
        {
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            GroupId2 = "02cf243d-d8b6-414e-a1e7-d631b656c976";
            EventSourceId = GroupId;
            ReadModelId = GroupId;

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId2, new GroupCreated(GroupName2)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId2, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserOnboarded()));
        }

        async Task Because()
        {
            ReadModel1 = await GetReadModel(GroupId);
            ReadModel2 = await GetReadModel(GroupId2);
        }
    }

    [Fact] void should_return_first_model() => Context.ReadModel1.ShouldNotBeNull();
    [Fact] void should_return_second_model() => Context.ReadModel2.ShouldNotBeNull();
    [Fact] void should_have_correct_group_name_on_model_1() => Context.ReadModel1.Name.ShouldEqual(GroupName);
    [Fact] void should_have_correct_group_name_on_model_2() => Context.ReadModel2.Name.ShouldEqual(GroupName2);
    [Fact] void should_have_user_id_on_child_on_model_1() => Context.ReadModel1.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_id_on_child_on_model_2() => Context.ReadModel2.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_name_on_child_on_model_1() => Context.ReadModel1.Users.First().Name.ShouldEqual(UserName);
    [Fact] void should_have_user_name_on_child_on_model_2() => Context.ReadModel2.Users.First().Name.ShouldEqual(UserName);
    [Fact] void should_have_user_onboarded_on_child_on_model_1() => Context.ReadModel1.Users.First().Onboarded.ShouldBeTrue();
    [Fact] void should_have_user_onboarded_on_child_on_model_2() => Context.ReadModel2.Users.First().Onboarded.ShouldBeTrue();
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
