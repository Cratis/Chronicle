// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Chronicle.Compatibility;

/// <summary>
/// Orders two protocol versions - the release-stamped major.minor.patch string both the .NET client and the
/// kernel report as their <c language="csharp">Cratis.Chronicle.Contracts.ProtocolVersion.Current</c>.
/// </summary>
static class ProtocolVersionOrder
{
    /// <summary>
    /// Determines whether a protocol version identifies a stamped build with a parseable numeric core.
    /// </summary>
    /// <param name="version">The protocol version.</param>
    /// <returns>Whether the version can be used to establish ordering on connect.</returns>
    public static bool IsKnown(string version) =>
        !string.IsNullOrWhiteSpace(version) &&
        version is not ("1.0.0" or "1.0.0.0") &&
        Version.TryParse(Core(version), out var parsed) && parsed.Build >= 0;

    /// <summary>
    /// Compares two protocol versions.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns>A negative number when <paramref name="left"/> is older, zero when they are the same, a positive number when it is newer.</returns>
    /// <remarks>
    /// Numeric components determine ordering, ignoring build metadata. For equal cores, a prerelease sorts
    /// below a release; two prereleases compare as equal without comparing their identifiers.
    /// An unparseable or missing component still reads as zero rather than throwing, but callers must use
    /// <see cref="IsKnown"/> before relying on the ordering to choose a compatibility direction.
    /// </remarks>
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

        return IsPrerelease(right).CompareTo(IsPrerelease(left));
    }

    static string Core(string version) => version.Split(['-', '+'], 2)[0];

    static bool IsPrerelease(string version) => version.Split('+', 2)[0].Contains('-');

    static int[] Components(string version) =>
        [.. Core(version).Split('.').Select(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)];

    static int At(int[] components, int index) => index < components.Length ? components[index] : 0;
}
