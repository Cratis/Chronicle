// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.Models;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_removing.one_child_of_two_removed.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_removing;

[Collection(GlobalCollection.Name)]
public class one_child_of_two_removed(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<UserProjection, User>(globalFixture)
    {
        public EventSourceId FirstGroupId;
        public EventSourceId SecondGroupId;
        public override IEnumerable<Type> EventTypes => [typeof(GroupCreated), typeof(UserCreated), typeof(UserAddedToGroup), typeof(UserRemovedFromGroup)];

        void Establish()
        {
            var userId = (EventSourceId)Guid.NewGuid();
            FirstGroupId = Guid.NewGuid();
            SecondGroupId = Guid.NewGuid();
            EventSourceId = userId;
            ModelId = userId;

            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new GroupCreated("SomeGroup")));
            EventsWithEventSourceIdToAppend.Add(new(SecondGroupId, new GroupCreated("SomeOtherGroup")));
            EventsWithEventSourceIdToAppend.Add(new(userId, new UserCreated("Someone")));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserAddedToGroup(userId)));
            EventsWithEventSourceIdToAppend.Add(new(SecondGroupId, new UserAddedToGroup(userId)));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserRemovedFromGroup(userId)));
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_only_have_one_child() => Context.Result.Groups.Count().ShouldEqual(1);
    [Fact] void should_have_the_correct_group_left() => Context.Result.Groups.First().GroupId.ShouldEqual(Context.SecondGroupId);
    [Fact] void should_not_have_the_removed_group() => Context.Result.Groups.Any(_ => _.GroupId == Context.FirstGroupId).ShouldBeFalse();
}
