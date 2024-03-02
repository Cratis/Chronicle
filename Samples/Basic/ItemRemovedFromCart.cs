// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Events;

namespace Basic;

[EventType("b2581e78-eff8-4609-b166-6d0387d0f149")]
public record ItemRemovedFromCart(PersonId PersonId, MaterialId MaterialId);
