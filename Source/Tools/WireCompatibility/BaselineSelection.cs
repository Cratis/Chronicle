// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// The released baselines to compare and the releases excluded by each bound.
/// </summary>
/// <param name="Baselines">The releases to compare, in released order.</param>
/// <param name="BeforeFloor">The releases earlier than the floor.</param>
/// <param name="AfterCeiling">The releases later than the ceiling.</param>
public record BaselineSelection(IReadOnlyList<string> Baselines, IReadOnlyList<string> BeforeFloor, IReadOnlyList<string> AfterCeiling)
{
    /// <summary>
    /// Selects baselines using an inclusive floor and ceiling and explicit withdrawals.
    /// </summary>
    /// <param name="released">The released versions in comparison order.</param>
    /// <param name="since">The optional inclusive floor.</param>
    /// <param name="upTo">The optional inclusive ceiling.</param>
    /// <param name="withdrawn">The released versions whose contracts were withdrawn.</param>
    /// <returns>The selected baselines and the releases outside each bound, evaluated independently.</returns>
    public static BaselineSelection From(IReadOnlyList<string> released, string? since, string? upTo, IReadOnlyCollection<string> withdrawn)
    {
        var beforeFloor = released.Where(_ => since is not null && !ReleaseVersion.IsAtOrAfter(_, since)).ToArray();
        var afterCeiling = released.Where(_ => upTo is not null && !ReleaseVersion.IsAtOrBefore(_, upTo)).ToArray();
        var baselines = released.Where(_ => !beforeFloor.Contains(_) && !afterCeiling.Contains(_) && !withdrawn.Contains(_)).ToArray();
        return new(baselines, beforeFloor, afterCeiling);
    }
}
