// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class an_order_with_a_root_from_and_child_join : an_order_projection_grain_with_lines_joined_to_products
{
    protected AppendedEvent[] _events;

    protected override string Declaration => base.Declaration.Replace("  from OrderShipped", "  from ProductRenamed\n  from OrderShipped", StringComparison.Ordinal);

    async Task Establish() => _events =
    [
        await OrderPlaced("Ada"),
        await ItemAddedToOrder(Kettle, 1),
        await ProductRenamed(Kettle, "Electric kettle")
    ];
}
