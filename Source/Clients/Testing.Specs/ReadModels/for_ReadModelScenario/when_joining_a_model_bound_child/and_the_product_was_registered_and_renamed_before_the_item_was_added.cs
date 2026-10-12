// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels.for_ReadModelScenario.when_joining_a_model_bound_child;

public class and_the_product_was_registered_and_renamed_before_the_item_was_added : Specification
{
    ReadModelScenario<Order> _scenario;
    EventSourceId _orderId;
    Guid _productId;
    Order? _order;

    void Establish()
    {
        _scenario = new ReadModelScenario<Order>();
        _orderId = new EventSourceId(Guid.NewGuid());
        _productId = Guid.NewGuid();
    }

    async Task Because()
    {
        await _scenario.Given.ForEventSource(new EventSourceId(_productId))
            .Events(new ProductRegistered("Kettle"), new ProductRenamed("Electric kettle"));
        await _scenario.Given.ForEventSource(_orderId)
            .Events(new OrderPlaced("Ada"), new ItemAddedToOrder(_productId, 1));

        _order = _scenario.InstanceForEventSourceId(_orderId);
    }

    [Fact] void should_materialize_the_order() => _order.ShouldNotBeNull();
    [Fact] void should_keep_the_customer_name() => _order!.CustomerName.ShouldEqual("Ada");
    [Fact] void should_create_exactly_one_line() => _order!.Lines.Count().ShouldEqual(1);
    [Fact] void should_keep_the_product_id() => _order!.Lines.Single().ProductId.ShouldEqual(_productId);
    [Fact] void should_keep_the_quantity() => _order!.Lines.Single().Quantity.ShouldEqual(1);
    [Fact] void should_backfill_the_latest_product_name() => _order!.Lines.Single().ProductName.ShouldEqual("Electric kettle");
    [Fact] void should_not_create_an_order_for_the_product() => _scenario.Instances.Count.ShouldEqual(1);
}
