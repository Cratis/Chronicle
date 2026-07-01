// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose <see cref="Region"/> is explicitly sourced from the order event, while <see cref="City"/>
/// is joined in from <see cref="RegionCustomerRegistered"/> — which <b>also</b> carries a colliding
/// <c>Region</c>. Used to verify that a join's name-based AutoMap does not bleed the joined event's
/// same-named value over an explicitly-sourced property.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Region">The order's region, explicitly sourced from the order event.</param>
/// <param name="City">The customer's city, joined in from <see cref="RegionCustomerRegistered"/>.</param>
[Passive]
[FromEvent<RegionOrderPlaced>]
public record RegionOrderSummary(
    [Key] Guid Id,

    [SetFrom<RegionOrderPlaced>]
    RegionCustomerId CustomerId,

    [SetFrom<RegionOrderPlaced>(nameof(RegionOrderPlaced.Region))]
    string Region,

    [Join<RegionCustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(RegionCustomerRegistered.City))]
    string City);
