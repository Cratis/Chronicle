// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_from_an_event_at_a_later_generation.and_properties_are_auto_mapped.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_from_an_event_at_a_later_generation;

[Collection(ChronicleCollection.Name)]
public class and_properties_are_auto_mapped(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid MemberId;
        public EnrolledMember Result;

        public override IEnumerable<Type> EventTypes => [typeof(MemberEnrolledV1), typeof(MemberEnrolled)];

        public override IEnumerable<Type> EventTypeMigrators => [typeof(MemberEnrolledMigrator)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(EnrolledMember)];

        async Task Because()
        {
            MemberId = Guid.Parse("3c1e5f7a-4b2d-4e8f-9a6b-1d2c3e4f5a6b");

            var projectionId = EventStore.Projections.GetProjectionIdForModel<EnrolledMember>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            var appendResult = await EventStore.EventLog.Append(MemberId.ToString(), new MemberEnrolled("Jane", "Smith"));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<EnrolledMember>(MemberId.ToString());
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_auto_map_the_first_name() => Context.Result.FirstName.ShouldEqual("Jane");
    [Fact] void should_auto_map_the_last_name() => Context.Result.LastName.ShouldEqual("Smith");
}
