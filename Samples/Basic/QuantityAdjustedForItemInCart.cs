// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Basic;

[EventType("f3927a33-7028-4242-bc06-a06f8ad62b68")]
public record QuantityAdjustedForItemInCart(PersonId PersonId, MaterialId MaterialId, int Quantity);
