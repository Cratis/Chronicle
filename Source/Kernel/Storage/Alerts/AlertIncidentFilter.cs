// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Alerts;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents filters applied before paging.
/// </summary>
/// <param name="Scope">Required scope.</param>
/// <param name="ObserverId">Optional affected observer.</param>
/// <param name="Condition">Optional recorded condition.</param>
/// <param name="MinimumSeverity">Optional minimum severity.</param>
public record AlertIncidentFilter(
    AlertIncidentScope Scope,
    ObserverId? ObserverId,
    AlertConditionKind? Condition,
    AlertSeverity? MinimumSeverity);
