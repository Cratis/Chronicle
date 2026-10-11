// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using context = Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_child_join_without_on.and_the_product_was_registered_and_renamed_before_the_item_was_added.context;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_child_join_without_on;

[Collection(ChronicleCollection.Name)]
public class and_the_product_was_registered_and_renamed_before_the_item_was_added(context context) : Given<context>(context)
{
    public class context(ChronicleFixture fixture) : Specification(fixture)
    {
        public Guid OrderId;
        public Guid ProductId;
        public Order Result;

        public override IEnumerable<Type> EventTypes => [typeof(ProductRegistered), typeof(ProductRenamed), typeof(OrderPlaced), typeof(ItemAddedToOrder)];
        public override IEnumerable<Type> ModelBoundProjections => [typeof(Order)];

        async Task Because()
        {
            OrderId = Guid.NewGuid();
            ProductId = Guid.NewGuid();
            var projectionId = EventStore.Projections.GetProjectionIdForModel<Order>();
            var handler = EventStore.Projections.GetAllHandlers().Single(_ => _.Id == projectionId);
            await handler.WaitTillSubscribed();

            await EventStore.EventLog.Append(ProductId.ToString(), new ProductRegistered("Kettle"));
            await EventStore.EventLog.Append(ProductId.ToString(), new ProductRenamed("Electric kettle"));
            await EventStore.EventLog.Append(OrderId.ToString(), new OrderPlaced("Ada"));
            var appendResult = await EventStore.EventLog.Append(OrderId.ToString(), new ItemAddedToOrder(ProductId, 1));
            await handler.WaitTillReachesEventSequenceNumber(appendResult.SequenceNumber);

            Result = await EventStore.ReadModels.GetInstanceById<Order>(OrderId.ToString());
        }
    }

    [Fact] void should_materialize_the_order() => Context.Result.ShouldNotBeNull();
    [Fact] void should_keep_the_customer_name() => Context.Result.CustomerName.ShouldEqual("Ada");
    [Fact] void should_create_exactly_one_line() => Context.Result.Lines.Count().ShouldEqual(1);
    [Fact] void should_keep_the_product_id() => Context.Result.Lines.Single().ProductId.ShouldEqual(Context.ProductId);
    [Fact] void should_keep_the_quantity() => Context.Result.Lines.Single().Quantity.ShouldEqual(1);
    [Fact] void should_backfill_the_latest_product_name() => Context.Result.Lines.Single().ProductName.ShouldEqual("Electric kettle");
}
