// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Specifications.Projections.Concepts;
using Cratis.Chronicle.Integration.Specifications.Projections.Events;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_removing.last_child_removed.context;

namespace Cratis.Chronicle.Integration.Specifications.Projections.Scenarios.when_removing;

[Collection(ChronicleCollection.Name)]
public class last_child_removed(context context) : Given<context>(context)
{
    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<GroupProjection, Group>(chronicleInProcessFixture)
    {
        public EventSourceId FirstGroupId;
        public override IEnumerable<Type> EventTypes => [typeof(GroupCreated), typeof(UserCreated), typeof(UserAddedToGroup), typeof(UserRemovedFromGroup)];

        public Group[] Groups;

        void Establish()
        {
            var userId = new UserId(Guid.NewGuid());
            FirstGroupId = Guid.NewGuid();
            EventSourceId = userId.ToString();
            ReadModelId = FirstGroupId;

            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new GroupCreated("SomeGroup")));
            EventsWithEventSourceIdToAppend.Add(new(userId.ToString(), new UserCreated("Someone")));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserAddedToGroup(userId)));
            EventsWithEventSourceIdToAppend.Add(new(FirstGroupId, new UserRemovedFromGroup(userId)));
        }

        async Task Because()
        {
            var result = await ChronicleInProcessFixture.ReadModels.Database.GetCollection<Group>().FindAsync(_ => true);
            Groups = result.ToList().ToArray();
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_no_children() => Context.Result.Users.ShouldBeEmpty();
    [Fact] void should_only_have_one_group() => Context.Groups.Length.ShouldEqual(1);
    [Fact] void should_only_have_the_correct_group() => Context.Groups[0].Id.Value.ShouldEqual(Context.ReadModelId);
}
