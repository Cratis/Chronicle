// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.Events;
using Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.ReadModels;
using context = Cratis.Chronicle.Integration.Client.Projections.Scenarios.ModelBound.when_removing_with_empty_event.and_item_is_removed.context;

namespace Cratis.Chronicle.Integration.Client.Projections.Scenarios.ModelBound.when_removing_with_empty_event;

[Collection(ChronicleCollection.Name)]
public class and_item_is_removed(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid ItemId;
        public ItemWithEmptyRemovalReadModel Result;

        public override IEnumerable<Type> EventTypes =>
            [typeof(ItemDefinedWithEmptyRemoval), typeof(ItemRemovedWithEmpty)];

        public override IEnumerable<Type> ModelBoundProjections => [typeof(ItemWithEmptyRemovalReadModel)];

        async Task Because()
        {
            ItemId = Guid.NewGuid();

            var projectionId = EventStore.Projections.GetProjectionIdForModel<ItemWithEmptyRemovalReadModel>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(ItemId, new ItemDefinedWithEmptyRemoval("Test Item"));
            var appendResult = await EventStore.EventLog.Append(ItemId, new ItemRemovedWithEmpty());

            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<ItemWithEmptyRemovalReadModel>(ItemId.ToString());
        }
    }

    [Fact] void should_have_removed_the_item() => Context.Result.ShouldEqual(default);
}
