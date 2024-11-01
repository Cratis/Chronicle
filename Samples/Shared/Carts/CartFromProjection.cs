// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Shared.Carts;

public record CartFromProjection(CartId Id, IEnumerable<CartItem> Items);
