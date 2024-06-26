// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Basic;

[EventType("147077c9-3954-4931-9a29-ea750bff97c1")]
public record ItemAddedToCart(PersonId PersonId, MaterialId MaterialId, int Quantity, Price? Price, Description? Description);
