// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Shared.Carts;

namespace Shared.Orders;

public record OrderState(int Items, IEnumerable<CartItem> CartItems);
