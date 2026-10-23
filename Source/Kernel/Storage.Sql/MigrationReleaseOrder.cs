// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Globalization;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Orders migration identifiers by the release they were written for.
/// </summary>
/// <remarks>
/// Identifiers end with the release, as in NS-Observers-v19_83_1_1. Compared as text, a three-digit minor version sorts before a
/// two-digit one and the table name decides before the release, so the order a fresh database was migrated in would not be the order the
/// releases were made in. Identifiers are compared by their release, number by number, and by the whole identifier within one release.
/// An identifier without a release sorts before every identifier with one.
/// </remarks>
public sealed class MigrationReleaseOrder : IComparer<string>, IComparer
{
    /// <summary>
    /// Gets the shared instance.
    /// </summary>
    public static readonly MigrationReleaseOrder Instance = new();

    const string ReleaseMarker = "-v";

    /// <summary>
    /// Try to get the release a migration identifier was written for.
    /// </summary>
    /// <param name="migrationId">The migration identifier.</param>
    /// <param name="release">The numbers of the release, most significant first.</param>
    /// <returns>True if the identifier ends with a release; false if not.</returns>
    public static bool TryGetRelease(string migrationId, out int[] release)
    {
        release = [];
        var marker = migrationId.LastIndexOf(ReleaseMarker, StringComparison.Ordinal);
        if (marker < 0)
        {
            return false;
        }

        var segments = migrationId[(marker + ReleaseMarker.Length)..].Split('_');
        var numbers = new int[segments.Length];
        for (var index = 0; index < segments.Length; index++)
        {
            if (!int.TryParse(segments[index], NumberStyles.None, CultureInfo.InvariantCulture, out numbers[index]))
            {
                return false;
            }
        }

        release = numbers;
        return true;
    }

    /// <inheritdoc/>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xHasRelease = TryGetRelease(x, out var xRelease);
        var yHasRelease = TryGetRelease(y, out var yRelease);
        if (xHasRelease != yHasRelease)
        {
            return xHasRelease ? 1 : -1;
        }

        var byRelease = CompareReleases(xRelease, yRelease);
        return byRelease != 0 ? byRelease : string.CompareOrdinal(x, y);
    }

    /// <inheritdoc/>
    int IComparer.Compare(object? x, object? y) => Compare(x as string, y as string);

    static int CompareReleases(int[] x, int[] y)
    {
        for (var index = 0; index < Math.Max(x.Length, y.Length); index++)
        {
            var left = index < x.Length ? x[index] : 0;
            var right = index < y.Length ? y[index] : 0;
            if (left != right)
            {
                return left.CompareTo(right);
            }
        }

        return x.Length.CompareTo(y.Length);
    }
}
