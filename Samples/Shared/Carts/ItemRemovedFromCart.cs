// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Shared.Customers;
using Shared.Products;

namespace Shared.Carts;

[EventType]
public record ItemRemovedFromCart(PersonId PersonId, MaterialId MaterialId);
