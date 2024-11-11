// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children.with_multiple_children_with_same_id.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_projecting_with_join_for_children;

[Collection(GlobalCollection.Name)]
public class with_multiple_children_with_same_id(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string GroupName2 = "Group2";
    const string UserName = "User";

    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<GroupProjectionWithMultipleJoins, Group>(globalFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public EventSourceId GroupId2;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(SystemUserCreated), typeof(GroupCreated), typeof(UserAddedToGroup), typeof(UserOnboarded), typeof(UserOffboarded), typeof(UserDetailsChanged)];
        public Group Model1;
        public Group Model2;
        void Establish()
        {
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            GroupId2 = "02cf243d-d8b6-414e-a1e7-d631b656c976";
            EventSourceId = GroupId;
            ModelId = GroupId;

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId2, new GroupCreated(GroupName2)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId2, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserOnboarded()));
        }

        async Task Because()
        {
            Model1 = await GetModel(GroupId);
            Model2 = await GetModel(GroupId2);
        }
    }

    [Fact] void should_return_first_model() => Context.Model1.ShouldNotBeNull();
    [Fact] void should_return_second_model() => Context.Model2.ShouldNotBeNull();
    [Fact] void should_have_correct_group_name_on_model_1() => Context.Model1.Name.ShouldEqual(GroupName);
    [Fact] void should_have_correct_group_name_on_model_2() => Context.Model2.Name.ShouldEqual(GroupName2);
    [Fact] void should_have_user_id_on_child_on_model_1() => Context.Model1.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_id_on_child_on_model_2() => Context.Model2.Users.First().UserId.ShouldEqual(Context.UserId);
    [Fact] void should_have_user_name_on_child_on_model_1() => Context.Model1.Users.First().Name.ShouldEqual(UserName);
    [Fact] void should_have_user_name_on_child_on_model_2() => Context.Model2.Users.First().Name.ShouldEqual(UserName);
    [Fact] void should_have_user_onboarded_on_child_on_model_1() => Context.Model1.Users.First().Onboarded.ShouldBeTrue();
    [Fact] void should_have_user_onboarded_on_child_on_model_2() => Context.Model2.Users.First().Onboarded.ShouldBeTrue();
}
