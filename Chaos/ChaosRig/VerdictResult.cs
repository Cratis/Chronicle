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
/// <param name="ReactorDuplicates">The informational at-least-once reactor redelivery count.</param>
/// <param name="Violations">The diagnostic violations.</param>
internal sealed record VerdictResult(int Lost, int Duplicated, int Gaps, int ObserversBehind, int ProjectionsBehind, int ReducersBehind, int FailedPartitions, int ReactorDuplicates, IReadOnlyList<string> Violations)
{
    /// <summary>
    /// Gets why a finding-free snapshot could not establish a stable drain.
    /// </summary>
    public string InconclusiveReason { get; init; } = string.Empty;

    /// <summary>
    /// Gets whether a durability or liveness invariant failed, excluding legitimate redelivery.
    /// </summary>
    public bool HasFindings => Lost + Duplicated + Gaps + ObserversBehind + ProjectionsBehind + ReducersBehind + FailedPartitions != 0 || Violations.Count != 0;

    /// <summary>
    /// Gets whether every invariant passed and the durable progress window was observed.
    /// </summary>
    public bool IsClean => !HasFindings && InconclusiveReason.Length == 0;

    /// <summary>
    /// Gets zero for clean, one for actual findings, or two for an inconclusive stability window.
    /// </summary>
    public int ExitCode => (HasFindings, IsClean) switch
    {
        (true, _) => 1,
        (_, true) => 0,
        _ => 2
    };
}
