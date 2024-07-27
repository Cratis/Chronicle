// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Orleans;

[EventType]
public record ItemAddedToCart(PersonId PersonId, MaterialId MaterialId, int Quantity, Price? Price, Description? Description);
