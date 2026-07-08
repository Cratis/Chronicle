// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model keyed by a <see cref="System.Guid"/> that joins a company name in from
/// <see cref="CompanyRegistered"/> — whose event source is a <see cref="OrgNumber">string</see>. Verifies a
/// join whose SOURCE is keyed by a string concept rather than a Guid.
/// </summary>
/// <param name="Id">The engagement identifier.</param>
/// <param name="CustomerOrgNumber">The company's organization number (the string join key).</param>
/// <param name="CustomerName">The company name, joined in from <see cref="CompanyRegistered"/>.</param>
[Passive]
[FromEvent<EngagementStarted>]
public record EngagementSummary(
    [Key] Guid Id,

    [SetFrom<EngagementStarted>]
    OrgNumber CustomerOrgNumber,

    [Join<CompanyRegistered>(on: nameof(CustomerOrgNumber), eventPropertyName: nameof(CompanyRegistered.Name))]
    string CustomerName);
