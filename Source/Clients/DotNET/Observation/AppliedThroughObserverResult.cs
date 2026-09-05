// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents the typed outcome for one requested observer.
/// </summary>
/// <param name="ObserverId">The <see cref="Observation.ObserverId"/> this outcome is for.</param>
/// <param name="Outcome">The <see cref="AppliedThroughOutcome"/> for this observer.</param>
public record AppliedThroughObserverResult(ObserverId ObserverId, AppliedThroughOutcome Outcome);
