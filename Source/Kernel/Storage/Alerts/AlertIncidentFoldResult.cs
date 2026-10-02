// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Alerts;

/// <summary>
/// Represents the result of folding a transition.
/// </summary>
/// <param name="Incident">Resulting row, if one exists.</param>
/// <param name="Outcome">Application outcome.</param>
public record AlertIncidentFoldResult(
    AlertIncident? Incident,
    AlertIncidentWriteOutcome Outcome);
