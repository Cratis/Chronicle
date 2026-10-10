// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// The direction of an intentional difference.
/// </summary>
public enum ConformanceDirection
{
    /// <summary>Chronicle accepts the declaration; Screenplay's semantic model rejects it on purpose.</summary>
    ScreenplayRejects = 0,

    /// <summary>Screenplay's semantic model accepts the declaration; Chronicle rejects it on purpose.</summary>
    ChronicleRejects = 1,

    /// <summary>Both reject the declaration, so there is nothing to compare.</summary>
    BothReject = 2,

    /// <summary>Both accept the declaration and lower it differently on purpose.</summary>
    LoweringDiffers = 3,

    /// <summary>The two sides disagree and it is not known to be intentional; the reason names the issue tracking it.</summary>
    Disagreement = 4
}
