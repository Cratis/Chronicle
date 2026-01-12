// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Concepts;
using Cratis.Chronicle.InProcess.Integration.Projections.Events;
using Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.ReadModels;
using MongoDB.Driver;
using context = Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_join_for_children.with_from_every.context;

namespace Cratis.Chronicle.InProcess.Integration.Projections.Scenarios.when_projecting_with_join_for_children;

[Collection(ChronicleCollection.Name)]
public class with_from_every(context context) : Given<context>(context)
{
    const string GroupName = "Group";
    const string UserName = "User";

    public class context(ChronicleInProcessFixture chronicleInProcessFixture) : given.a_projection_and_events_appended_to_it<GroupProjectionWithFromEvery, GroupWithLastUpdated>(chronicleInProcessFixture)
    {
        public UserId UserId;
        public EventSourceId GroupId;
        public override IEnumerable<Type> EventTypes => [typeof(UserCreated), typeof(GroupCreated), typeof(UserAddedToGroup)];
        public GroupWithLastUpdated ReadModel;

        void Establish()
        {
            UserId = Guid.Parse("3c760aaf-2119-4336-8721-3f4c97e86a1b");
            GroupId = "462ec4f6-fd9e-4549-92b9-00b769636468";
            EventSourceId = GroupId;
            ReadModelId = GroupId;

            EventsWithEventSourceIdToAppend.Add(new(GroupId, new GroupCreated(GroupName)));
            EventsWithEventSourceIdToAppend.Add(new(GroupId, new UserAddedToGroup(UserId)));
            EventsWithEventSourceIdToAppend.Add(new(UserId.ToString(), new UserCreated(UserName)));
        }

        async Task Because()
        {
            var result = await ChronicleFixture.ReadModels.Database.GetCollection<GroupWithLastUpdated>().FindAsync(_ => _.Id == UserId.ToString());
            ReadModel = await result.FirstOrDefaultAsync();
        }
    }

    [Fact] void should_not_create_instance_for_identity_of_joined_type() => Context.ReadModel.ShouldBeNull();
    [Fact] void should_set_the_event_sequence_number_to_last_event() => Context.Result.__lastHandledEventSequenceNumber.ShouldEqual(Context.LastEventSequenceNumber);
}
