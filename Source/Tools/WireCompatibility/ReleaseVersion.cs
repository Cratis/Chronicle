// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Orders released versions so a floor can say which of them still count as baselines.
/// </summary>
/// <remarks>
/// Only stable releases reach this - the version list is already filtered to those - so there is no pre-release
/// ordering to get right, and comparing the three numeric components is the whole of it.
/// </remarks>
public static class ReleaseVersion
{
    /// <summary>
    /// Determines whether a released version is at or after a floor.
    /// </summary>
    /// <param name="version">The released version.</param>
    /// <param name="floor">The floor.</param>
    /// <returns>True when the version is at or after the floor.</returns>
    public static bool IsAtOrAfter(string version, string floor) => Compare(version, floor) >= 0;

    /// <summary>
    /// Compares two released versions.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns>A negative number when the first is older, zero when they are the same, a positive number when it is newer.</returns>
    public static int Compare(string left, string right)
    {
        var first = Components(left);
        var second = Components(right);

        for (var index = 0; index < Math.Max(first.Length, second.Length); index++)
        {
            var comparison = At(first, index).CompareTo(At(second, index));
            if (comparison != 0)
            {
                return comparison;
            }
        }

        return 0;
    }

    static int[] Components(string version) =>
        [.. version.Split('.').Select(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)];

    static int At(int[] components, int index) => index < components.Length ? components[index] : 0;
}
