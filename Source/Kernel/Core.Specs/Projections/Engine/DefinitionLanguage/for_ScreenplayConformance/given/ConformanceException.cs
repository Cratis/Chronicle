// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// One intentional difference.
/// </summary>
/// <param name="Direction">How the two sides differ.</param>
/// <param name="Reason">Why the difference exists, or the issue tracking it.</param>
public record ConformanceException(ConformanceDirection Direction, string Reason)
{
    /// <summary>
    /// Checks whether an outcome is the one this entry records.
    /// </summary>
    /// <param name="outcome">The outcome of comparing the case.</param>
    /// <returns>True when the outcome still differs the way the entry says.</returns>
    public bool Matches(ConformanceOutcome outcome) => Direction switch
    {
        ConformanceDirection.ScreenplayRejects => outcome == ConformanceOutcome.ScreenplayRejects,
        ConformanceDirection.ChronicleRejects => outcome == ConformanceOutcome.ChronicleRejects,
        ConformanceDirection.BothReject => outcome == ConformanceOutcome.BothReject,
        ConformanceDirection.LoweringDiffers => outcome == ConformanceOutcome.Differs,
        ConformanceDirection.Disagreement => outcome != ConformanceOutcome.Equivalent,
        _ => false
    };
}
