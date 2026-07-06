// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Test event registering a partner on their own event source, joined into orders for their name. It also
/// carries a <see cref="Status"/> that collides by name with the order status.
/// </summary>
/// <param name="Name">The partner's name (joined in).</param>
/// <param name="Status">The partner's status — deliberately named to collide with the order status.</param>
[EventType]
public record PartnerRegistered(string Name, string Status);
