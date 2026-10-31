// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class two_orders_joined_to_a_product : an_order_projection_grain_with_lines_joined_to_products
{
    protected const string SecondOrder = "00000000-0000-0000-0000-000000000002";
    protected AppendedEvent[] _events;

    protected override string Declaration => """
        projection Order => Proof.Pdl.Order
          every
            LastUpdated = $eventContext.occurred
          from OrderPlaced
            CustomerName = CustomerName
            ProductId = ProductId
          join Product on ProductId
            with ProductRenamed
              ProductName = Name
        """;

    async Task Establish() => _events =
    [
        await Append(Order, "OrderPlaced", new { CustomerName = "Ada", ProductId = Guid.Parse(Kettle) }),
        await Append(SecondOrder, "OrderPlaced", new { CustomerName = "Grace", ProductId = Guid.Parse(Kettle) }),
        await ProductRenamed(Kettle, "Electric kettle")
    ];
}
