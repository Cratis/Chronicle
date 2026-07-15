// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that enriches an order with the customer's name through a <c>[Join]</c>, where the join key
/// (<see cref="CustomerId"/>) is populated by AutoMap — it name-matches <see cref="JoinOrderPlaced.CustomerId"/>
/// and carries NO <c>[SetFrom]</c>. This is the reproduction shape for the row-creation-time join backfill:
/// the join key never appears in the explicit From mappings, only in the AutoMap-merged From properties.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer the order is for (the AutoMapped join key — no <c>[SetFrom]</c>).</param>
/// <param name="Amount">The order amount.</param>
/// <param name="CustomerName">The customer name, joined in from <see cref="JoinCustomerRegistered"/>.</param>
[Passive]
[FromEvent<JoinOrderPlaced>]
public record AutoMappedJoinOrderSummary(
    [Key] Guid Id,

    JoinCustomerId CustomerId,

    [SetFrom<JoinOrderPlaced>]
    decimal Amount,

    [Join<JoinCustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(JoinCustomerRegistered.Name))]
    string CustomerName);
