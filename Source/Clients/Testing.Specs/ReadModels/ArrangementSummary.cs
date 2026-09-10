// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Keys;
using Cratis.Chronicle.Projections;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// Read model whose <see cref="Location"/> is set exclusively from <see cref="ArrangementSet"/> and flagged
/// <c language="csharp">[NoAutoMap]</c>, while it also value-maps <see cref="WorkMode"/> from <see cref="WorkModeSet"/>. Because
/// <see cref="WorkModeSet"/> is subscribed for a real value mapping (not an aggregate), the aggregate
/// heuristic does not protect the location — this verifies that property-level <c language="csharp">[NoAutoMap]</c> is what
/// stops <see cref="WorkModeSet"/>'s identically named <c language="csharp">Location</c> from overwriting it.
/// </summary>
/// <param name="Id">Summary identifier.</param>
/// <param name="Location">The location, sourced only from <see cref="ArrangementSet"/>.</param>
/// <param name="WorkMode">The work mode, value-mapped from <see cref="WorkModeSet"/>.</param>
[Passive]
[FromEvent<ArrangementSet>]
public record ArrangementSummary(
    [Key] Guid Id,

    [SetFrom<ArrangementSet>]
    [NoAutoMap]
    string Location,

    [SetFrom<WorkModeSet>]
    string WorkMode);
