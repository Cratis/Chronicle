// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_with_join_and_no_auto_map;

/// <summary>
/// Model-bound read model whose <see cref="Status"/> is sourced only from <see cref="OrderPlacedForCustomer"/>
/// and flagged <c>[NoAutoMap]</c>, while it <c>[Join]</c>s a <see cref="PartnerRegisteredForOrder"/> that
/// carries an identically named <c>Status</c>. Against the real kernel the join resolves after the root
/// projection, so without the property-level <c>[NoAutoMap]</c> exclusion the partner status would overwrite
/// the order status.
/// </summary>
/// <param name="Id">Order identifier.</param>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Status">The order status, sourced only from <see cref="OrderPlacedForCustomer"/>.</param>
/// <param name="PartnerName">The partner name, joined in from <see cref="PartnerRegisteredForOrder"/>.</param>
[FromEvent<OrderPlacedForCustomer>]
public record OrderJoinSummary(
    Guid Id,

    [SetFrom<OrderPlacedForCustomer>]
    Guid CustomerId,

    [SetFrom<OrderPlacedForCustomer>]
    [NoAutoMap]
    string Status,

    [Join<PartnerRegisteredForOrder>(on: nameof(CustomerId), eventPropertyName: nameof(PartnerRegisteredForOrder.Name))]
    string PartnerName);
