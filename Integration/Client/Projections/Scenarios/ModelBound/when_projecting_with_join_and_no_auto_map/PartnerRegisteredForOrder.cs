// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_join_and_no_auto_map;

/// <summary>
/// Join-source event registering a partner on the customer's own event source. It also carries a
/// <see cref="Status"/> that collides by name with the order status.
/// </summary>
/// <param name="Name">The partner's name (joined in).</param>
/// <param name="Status">The partner's status — deliberately named to collide with the order status.</param>
[EventType]
public record PartnerRegisteredForOrder(string Name, string Status);
