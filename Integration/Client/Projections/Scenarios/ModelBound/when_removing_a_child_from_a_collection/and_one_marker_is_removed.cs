// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_removing_a_child_from_a_collection.and_one_marker_is_removed.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_removing_a_child_from_a_collection;

[Collection(ChronicleCollection.Name)]
public class and_one_marker_is_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid RegionId;
        public Guid FirstMarkerId;
        public Guid SecondMarkerId;
        public RegionReadModel Result;

        public override IEnumerable<Type> EventTypes =>
            [typeof(RegionDefined), typeof(MarkerPlacedInRegion), typeof(MarkerRemovedFromRegion)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(RegionReadModel)];

        async Task Because()
        {
            RegionId = Guid.NewGuid();
            FirstMarkerId = Guid.NewGuid();
            SecondMarkerId = Guid.NewGuid();

            var projectionId = EventStore.Projections.GetProjectionIdForModel<RegionReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(RegionId, new RegionDefined("North"));
            await EventStore.EventLog.Append(RegionId, new MarkerPlacedInRegion(RegionId, FirstMarkerId, "Camp"));
            await EventStore.EventLog.Append(RegionId, new MarkerPlacedInRegion(RegionId, SecondMarkerId, "Outpost"));
            var appendResult = await EventStore.EventLog.Append(RegionId, new MarkerRemovedFromRegion(RegionId, FirstMarkerId));

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<RegionReadModel>(RegionId.ToString());
        }
    }

    [Fact] void should_return_model() => Context.Result.ShouldNotBeNull();
    [Fact] void should_have_one_marker_left() => Context.Result.Markers.Count().ShouldEqual(1);
    [Fact] void should_keep_the_other_marker() => Context.Result.Markers.First().Id.ShouldEqual(Context.SecondMarkerId);
    [Fact] void should_not_have_the_removed_marker() => Context.Result.Markers.Any(_ => _.Id == Context.FirstMarkerId).ShouldBeFalse();
}
