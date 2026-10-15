// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#pragma warning disable SA1402 // File may only contain a single type

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_properties_from_every.and_a_child_only_event_updates_the_parent.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_properties_from_every;

[Collection(ChronicleCollection.Name)]
public class and_a_child_only_event_updates_the_parent(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public static readonly Guid OrderId = Guid.Parse("edcc36b4-c5df-418f-bd37-b248242d9a17");
        public static readonly Guid ItemId = Guid.Parse("253b206d-7c70-4c8a-97c4-dde2ae1b1201");
        public static readonly DateTimeOffset UpdatedAt = new(2026, 1, 3, 0, 0, 0, TimeSpan.Zero);
        public AuditedOrder Result;
        public IEnumerable<AuditedOrder> Instances;

        public override IEnumerable<Type> EventTypes => [typeof(AuditedOrderCreated), typeof(AuditedItemAdded), typeof(AuditedItemQuantityAdjusted)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(AuditedOrder)];

        async Task Because()
        {
            var projectionId = EventStore.Projections.GetProjectionIdForModel<AuditedOrder>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(OrderId, new AuditedOrderCreated("Ada"), occurred: UpdatedAt.AddDays(-2));
            await EventStore.EventLog.Append(OrderId, new AuditedItemAdded(ItemId, 1), occurred: UpdatedAt.AddDays(-1));
            var appendResult = await EventStore.EventLog.Append(OrderId, new AuditedItemQuantityAdjusted(ItemId, 3), occurred: UpdatedAt);
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<AuditedOrder>(OrderId.ToString());
            Instances = await EventStore.ReadModels.GetInstances<AuditedOrder>();
        }
    }

    [Fact] void should_update_the_parent_timestamp_from_the_child_only_event() => Context.Result.LastUpdated.ShouldEqual(context.UpdatedAt);
    [Fact] void should_update_the_existing_child() => Context.Result.Items.Single().Quantity.ShouldEqual(3);
    [Fact] void should_keep_the_child_identifier() => Context.Result.Items.Single().Id.ShouldEqual(context.ItemId);
    [Fact] void should_not_create_a_root_keyed_by_the_child() => Context.Instances.Select(_ => _.Id).ShouldContainOnly(context.OrderId);
}

[EventType]
public record AuditedOrderCreated(string Customer);

[EventType]
public record AuditedItemAdded(Guid ItemId, int Quantity);

[EventType]
public record AuditedItemQuantityAdjusted(Guid ItemId, int NewQuantity);

[FromEvent<AuditedOrderCreated>]
public record AuditedOrder(
    Guid Id,
    string Customer,
    [FromEvery(contextProperty: nameof(EventContext.Occurred))] DateTimeOffset LastUpdated,
    [ChildrenFrom<AuditedItemAdded>(key: nameof(AuditedItemAdded.ItemId))] IEnumerable<AuditedItem> Items);

[FromEvent<AuditedItemQuantityAdjusted>(key: nameof(AuditedItemQuantityAdjusted.ItemId))]
public record AuditedItem(
    [Key] Guid Id,
    [SetFrom<AuditedItemQuantityAdjusted>(nameof(AuditedItemQuantityAdjusted.NewQuantity))] int Quantity);

#pragma warning restore SA1402 // File may only contain a single type
