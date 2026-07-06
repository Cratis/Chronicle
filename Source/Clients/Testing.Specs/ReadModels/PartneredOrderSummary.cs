// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose <see cref="Status"/> is sourced only from <see cref="PartneredOrderPlaced"/> and flagged
/// <c>[NoAutoMap]</c>, while it <c>[Join]</c>s a <see cref="PartnerRegistered"/> that carries an identically
/// named <c>Status</c>. Verifies property-level <c>[NoAutoMap]</c> also excludes the property from the
/// join's AutoMap, so the joined partner status cannot overwrite the order status.
/// </summary>
/// <param name="Id">Order identifier.</param>
/// <param name="CustomerId">The customer/partner the order is for (the join key).</param>
/// <param name="Status">The order status, sourced only from <see cref="PartneredOrderPlaced"/>.</param>
/// <param name="PartnerName">The partner name, joined in from <see cref="PartnerRegistered"/>.</param>
[Passive]
[FromEvent<PartneredOrderPlaced>]
public record PartneredOrderSummary(
    [Key] Guid Id,

    [SetFrom<PartneredOrderPlaced>]
    JoinCustomerId CustomerId,

    [SetFrom<PartneredOrderPlaced>]
    [NoAutoMap]
    string Status,

    [Join<PartnerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(PartnerRegistered.Name))]
    string PartnerName);
