// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Chaos;

/// <summary>
/// Reports consistency failures and their diagnostic details.
/// </summary>
/// <param name="Lost">The missing acknowledged event count.</param>
/// <param name="Duplicated">The duplicate event count.</param>
/// <param name="Gaps">The sequence gap count.</param>
/// <param name="ObserversBehind">The observer progress failure count.</param>
/// <param name="ProjectionsBehind">The projection mismatch count.</param>
/// <param name="ReducersBehind">The reducer mismatch count.</param>
/// <param name="FailedPartitions">The failed partition count.</param>
/// <param name="ReactorDuplicates">The duplicate reactor delivery count.</param>
/// <param name="Violations">The diagnostic violations.</param>
internal sealed record VerdictResult(int Lost, int Duplicated, int Gaps, int ObserversBehind, int ProjectionsBehind, int ReducersBehind, int FailedPartitions, int ReactorDuplicates, IReadOnlyList<string> Violations)
{
    /// <summary>
    /// Gets whether every consistency check passed.
    /// </summary>
    public bool IsClean => Lost + Duplicated + Gaps + ObserversBehind + ProjectionsBehind + ReducersBehind + FailedPartitions + ReactorDuplicates == 0 && Violations.Count == 0;
}
