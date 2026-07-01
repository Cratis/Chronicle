// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model that enriches an order with the customer's name through a <c>[Join]</c> against
/// <see cref="JoinCustomerRegistered"/> on a separate event source. Used to verify cross-source join
/// behavior in the in-memory harness and to assert against the intended instance deterministically.
/// </summary>
/// <param name="Id">The order identifier.</param>
/// <param name="CustomerId">The customer the order is for (the join key).</param>
/// <param name="Amount">The order amount.</param>
/// <param name="CustomerName">The customer name, joined in from <see cref="JoinCustomerRegistered"/>.</param>
[Passive]
[FromEvent<JoinOrderPlaced>]
public record JoinOrderSummary(
    [Key] Guid Id,

    [SetFrom<JoinOrderPlaced>]
    JoinCustomerId CustomerId,

    [SetFrom<JoinOrderPlaced>]
    decimal Amount,

    [Join<JoinCustomerRegistered>(on: nameof(CustomerId), eventPropertyName: nameof(JoinCustomerRegistered.Name))]
    string CustomerName);
