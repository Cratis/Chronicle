// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Projections.for_Projection.when_processing_multiple_read_models;

public class and_children_follow_root_removal : given.an_order_projection_grain_with_lines_joined_to_products
{
    protected override string Declaration => """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
            CustomerName = CustomerName
            IsShipped = false
          children Lines identified by ProductId
            from ItemAddedToOrder key ProductId
              parent $eventContext.eventSourceId
              Quantity = Quantity
            join Product on ProductId
              with ProductRegistered
                ProductName = Name
          remove with OrderCancelled
        """;

    IEnumerable<ExpandoObject> _removed;
    IEnumerable<ExpandoObject> _afterChildren;
    IEnumerable<ExpandoObject> _recreated;

    async Task Because()
    {
        var created = await OrderPlaced("Ada");
        var removed = await Append(Order, "OrderCancelled", new { });
        var child = await ItemAddedToOrder(Kettle, 2);
        var joined = await ProductRegistered(Kettle, "Kettle");
        var recreated = await OrderPlaced("Grace");
        _removed = await _grain.Process(EventStoreNamespaceName.Default, [created, removed]);
        _afterChildren = await _grain.Process(EventStoreNamespaceName.Default, [created, removed, child, joined]);
        _recreated = await _grain.Process(EventStoreNamespaceName.Default, [created, removed, child, joined, recreated]);
    }

    [Fact] void should_hide_the_removed_root() => _removed.ShouldBeEmpty();
    [Fact] void should_not_reveal_the_root_on_child_or_join_events() => _afterChildren.ShouldBeEmpty();
    [Fact] void should_recreate_the_root_on_its_own_event() => ReadModelFor(_recreated, Order)["CustomerName"].ShouldEqual("Grace");
    [Fact] void should_keep_the_child_until_recreation() => LinesOf(ReadModelFor(_recreated, Order)).Single()["Quantity"].ShouldEqual(2);
}
