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
    /// Compares two protocol versions.
    /// </summary>
    /// <param name="left">The first version.</param>
    /// <param name="right">The second version.</param>
    /// <returns>A negative number when <paramref name="left"/> is older, zero when they are the same, a positive number when it is newer.</returns>
    /// <remarks>
    /// An unparsable or missing component reads as zero rather than throwing - a side that cannot say its own
    /// version is treated as the oldest possible one. That degrades to the direction Chronicle has always
    /// checked (the caller treated as the older side) rather than failing a connection over a version string
    /// it cannot make sense of.
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

        return 0;
    }

    static int[] Components(string version) =>
        [.. version.Split('.').Select(part => int.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)];

    static int At(int[] components, int index) => index < components.Length ? components[index] : 0;
}
