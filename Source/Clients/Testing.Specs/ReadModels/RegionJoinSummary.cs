// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose <see cref="Region"/> is sourced <b>only</b> by a <c>[Join]</c> against
/// <see cref="RegionCustomerRegistered"/>, even though the subscribed order event also carries a colliding
/// <c>Region</c>. Used to verify that name-based AutoMap from the order event does not bleed its value over
/// a join-sourced property — so when the join source is absent the property stays unset rather than taking
/// the order's coincidental region.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Region">The customer's region, joined in — never the order's own region.</param>
[Passive]
[FromEvent<RegionOrderPlaced>]
public record RegionJoinSummary(
    [Key] Guid Id,

    [SetFrom<RegionOrderPlaced>]
    RegionCustomerId CustomerId,

    [Join<RegionCustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(RegionCustomerRegistered.Region))]
    string? Region);
