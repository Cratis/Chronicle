// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Shared.Customers;
using Shared.Orders;
using Shared.Products;

namespace Shared.Carts;

[EventType]
public record ItemAddedToCart(PersonId PersonId, MaterialId MaterialId, int Quantity, Price? Price, Description? Description);
