// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Tools.WireCompatibility;

/// <summary>
/// Orders versions so a floor and ceiling can say which releases still count as baselines.
/// </summary>
/// <remarks>
/// Baselines are stable releases, but a ceiling can be a pre-release. Only the numeric core before a pre-release
/// suffix or build metadata is compared, so versions with the same core are equal.
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
    /// Determines whether a released version is at or before a ceiling.
    /// </summary>
    /// <param name="version">The released version.</param>
    /// <param name="ceiling">The inclusive ceiling.</param>
    /// <returns>True when the version is at or before the ceiling.</returns>
    public static bool IsAtOrBefore(string version, string ceiling) => Compare(version, ceiling) <= 0;

    /// <summary>
    /// Determines whether a version has three non-negative numeric components and an optional non-empty suffix.
    /// </summary>
    /// <param name="version">The version to validate.</param>
    /// <returns>True when the version is well formed.</returns>
    public static bool IsValid(string version)
    {
        var core = version.Split('-', '+')[0];
        var components = core.Split('.');
        return components.Length == 3 &&
            components.All(_ => int.TryParse(_, NumberStyles.None, CultureInfo.InvariantCulture, out var value) && value >= 0) &&
            (core.Length == version.Length || version.Length > core.Length + 1);
    }

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
        [.. version.Split('-', '+')[0].Split('.').Select(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)];

    static int At(int[] components, int index) => index < components.Length ? components[index] : 0;
}
