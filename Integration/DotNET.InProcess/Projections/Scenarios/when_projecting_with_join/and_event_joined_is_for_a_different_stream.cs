// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_join.and_event_joined_is_for_a_different_stream.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_join;

[Collection(ChronicleCollection.Name)]
public class and_event_joined_is_for_a_different_stream(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string UserName = "User";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<ProjectionWithJoinOnRoot, User>(chronicleInProcessFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(GroupCreated), typeof(UserAddedToGroup), typeof(UserDetailsChanged)];

        void Establish()
        {
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            EventSourceId = UserId.ToString();
            ReadModelId = UserId.ToString();
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            const string otherUserId = "564ac42d-66ba-444f-b265-fd42e900df75";

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(otherUserId)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_set_user_name() => Context.Result.Name.ShouldEqual(UserName);
    [Fact] void should_not_set_group_id() => Context.Result.GroupId.ShouldNotEqual(Context.GroupId);
    [Fact] void should_not_set_group_name() => Context.Result.GroupName.ShouldNotEqual(GroupName);
    [Fact] void should_keep_the_event_sequence_number_for_last_event_affecting_the_projection() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber - 1);
}
