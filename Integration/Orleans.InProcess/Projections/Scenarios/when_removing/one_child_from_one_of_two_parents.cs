// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Base;
using Cratis.Chronicle.Integration.Orleans.InProcess.AggregateRoots.Concepts;
using Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Events;
using MongoDB.Driver;
using context = Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_removing.one_child_from_one_of_two_parents.context;

namespace Cratis.Chronicle.Integration.Orleans.InProcess.Projections.Scenarios.when_removing;

[Collection(GlobalCollection.Name)]
public class one_child_from_one_of_two_parents(context context) : Given<context>(context)
{
    public class context(GlobalFixture globalFixture) : given.a_projection_and_events_appended_to_it<GroupProjection, Group>(globalFixture)
    {
        public EventSourceId FirstGroupId;
        public EventSourceId SecondGroupId;
        public override IEnumerable<Type> EventTypes => [typeof(GroupCreated), typeof(UserCreated), typeof(UserAddedToGroup), typeof(UserRemovedFromGroup)];

        public Group[] Groups;
        public EventSourceId[] ResultingGroupIds;

        void Establish()
        {
            var userId = new UserId(Guid.NewGuid());
            FirstGroupId = Guid.NewGuid();
            SecondGroupId = Guid.NewGuid();
            EventSourceId = userId.ToString();
            ModelId = FirstGroupId;

            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new GroupCreated("SomeGroup")));
            EventsWithEventSourceIdToAppend.Add(new(SecondGroupId, new GroupCreated("SomeGroup")));
            EventsWithEventSourceIdToAppend.Add(new(userId.ToString(), new UserCreated("Someone")));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserAddedToGroup(userId)));
            EventsWithEventSourceIdToAppend.Add(new(SecondGroupId, new UserAddedToGroup(userId)));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserRemovedFromGroup(userId)));
        }

        async Task Because()
        {
            var result = await _globalFixture.ReadModels.Database.GetCollection<Group>().FindAsync(_ => true);
            Groups = result.ToList().ToArray();
            ResultingGroupIds = Groups.Select(_ => (EventSourceId)_.Id.Value).ToArray();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_only_have_no_children_on_first_group() => Context.Result.Users.ShouldBeEmpty();
    [Fact] void should_only_have_one_child_on_second_group() => Context.Groups.First(_ => _.Id.Value == Context.SecondGroupId.Value).Users.Count().ShouldEqual(1);
    [Fact] void should_have_two_groups() => Context.Groups.Length.ShouldEqual(2);
    [Fact] void should_only_have_correct_two_groups() => Context.ResultingGroupIds.ShouldContainOnly([Context.FirstGroupId, Context.SecondGroupId]);
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
