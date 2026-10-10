// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.DeclarationLanguage.for_ScreenplayConformance.given;

/// <summary>
/// How Chronicle's and Screenplay's lowerings of one declaration compare.
/// </summary>
public enum ConformanceOutcome
{
    /// <summary>Both accept the declaration and lower it to the same neutral tree.</summary>
    Equivalent = 0,

    /// <summary>Both accept the declaration and lower it differently.</summary>
    Differs = 1,

    /// <summary>Chronicle accepts the declaration and Screenplay's semantic model rejects it.</summary>
    ScreenplayRejects = 2,

    /// <summary>Screenplay's semantic model accepts the declaration and Chronicle rejects it.</summary>
    ChronicleRejects = 3,

    /// <summary>Both reject the declaration.</summary>
    BothReject = 4
}
