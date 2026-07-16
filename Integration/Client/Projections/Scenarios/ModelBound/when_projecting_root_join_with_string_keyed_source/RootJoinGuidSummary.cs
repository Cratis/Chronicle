// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.Integration.Projections.Events;
using Cratis.Chronicle.Projections.ModelBound;

namespace Cratis.Chronicle.Integration.Projections.Scenarios.ModelBound.when_projecting_root_join_with_string_keyed_source;

/// <summary>
/// Faithful reproduction of Ada's CHR-18 shape: a Guid-keyed model-bound root read model whose single
/// root-level <c>[Join]</c> matches on a NON-Guid string property (<see cref="CustomerOrgNumber"/>), whose
/// join source (<see cref="CompanyRegistered"/>) is appended to a non-Guid org-number stream. When that
/// source arrives with no matching root, the join value must never be materialized as this model's Guid
/// key — otherwise the poison string <c>_id</c> is stored and the next read-back <c>Guid.Parse</c> throws.
/// </summary>
/// <param name="Id">The Guid engagement identifier (key schema format "guid").</param>
/// <param name="CustomerOrgNumber">The customer's organization number — the string join-on property.</param>
/// <param name="CustomerName">The customer name joined in from <see cref="CompanyRegistered"/>.</param>
/// <param name="__lastHandledEventSequenceNumber">The last handled event sequence number.</param>
[FromEvent<EngagementStarted>]
public record RootJoinGuidSummary(
    Guid Id,

    string CustomerOrgNumber,

    [Join<CompanyRegistered>(on: nameof(CustomerOrgNumber), eventPropertyName: nameof(CompanyRegistered.Name))]
    string CustomerName,

    EventSequenceNumber __lastHandledEventSequenceNumber);
