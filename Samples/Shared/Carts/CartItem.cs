// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Shared.Products;

namespace Shared.Carts;

public record CartItem(MaterialId MaterialId, int Quantity);
