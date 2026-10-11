// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_Projection.given;

public class an_order_joined_on_its_identifier : an_order_projection_grain_with_lines_joined_to_products
{
    protected override string Declaration => """
        projection Order => Proof.Pdl.Order
          from OrderPlaced
            CustomerName = CustomerName
          join Product on id
            with ProductRenamed
              ProductName = Name
        """;
}
