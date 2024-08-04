// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events.Constraints;

namespace Orleans;

[EventType]
[Unique]
public record ItemAddedToCart(PersonId PersonId, MaterialId MaterialId, int Quantity, Price? Price, Description? Description);
