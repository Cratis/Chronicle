// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Cratis.Chronicle.Concepts.System;

/// <summary>
/// Represents a semantic version.
/// </summary>
/// <param name="Major">Major version number.</param>
/// <param name="Minor">Minor version number.</param>
/// <param name="Patch">Patch version number.</param>
/// <param name="PreRelease">Optional pre-release identifier.</param>
/// <param name="BuildMetadata">Optional build metadata.</param>
public partial record SemanticVersion(int Major, int Minor, int Patch, string PreRelease = "", string BuildMetadata = "") : IComparable<SemanticVersion>, IComparable
{
    /// <summary>
    /// Gets an empty version (0.0.0).
    /// </summary>
    public static readonly SemanticVersion NotSet = new(0, 0, 0);

    /// <summary>
    /// Implicit conversion from string to <see cref="SemanticVersion"/>.
    /// </summary>
    /// <param name="version">Version string.</param>
    public static implicit operator SemanticVersion(string version) => Parse(version);

    /// <summary>
    /// Greater than operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is greater than right.</returns>
    public static bool operator >(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) > 0;

    /// <summary>
    /// Less than operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is less than right.</returns>
    public static bool operator <(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) < 0;

    /// <summary>
    /// Greater than or equal operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is greater than or equal to right.</returns>
    public static bool operator >=(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) >= 0;

    /// <summary>
    /// Less than or equal operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns>True if left is less than or equal to right.</returns>
    public static bool operator <=(SemanticVersion left, SemanticVersion right) =>
        left.CompareTo(right) <= 0;

    /// <summary>
    /// Parse a semantic version string.
    /// </summary>
    /// <param name="version">Version string to parse.</param>
    /// <returns>Parsed <see cref="SemanticVersion"/>.</returns>
    /// <exception cref="ArgumentException">Thrown if the version string is invalid.</exception>
    public static SemanticVersion Parse(string version)
    {
        var match = SemanticVersionRegex().Match(version);
        if (!match.Success)
        {
            throw new ArgumentException($"Invalid semantic version: {version}", nameof(version));
        }

        var major = int.Parse(match.Groups["major"].Value, CultureInfo.InvariantCulture);
        var minor = int.Parse(match.Groups["minor"].Value, CultureInfo.InvariantCulture);
        var patch = int.Parse(match.Groups["patch"].Value, CultureInfo.InvariantCulture);
        var preRelease = match.Groups["prerelease"].Value;
        var buildMetadata = match.Groups["buildmetadata"].Value;

        return new SemanticVersion(major, minor, patch, preRelease, buildMetadata);
    }

    /// <summary>
    /// Try to parse a semantic version string.
    /// </summary>
    /// <param name="version">Version string to parse.</param>
    /// <param name="result">The parsed <see cref="SemanticVersion"/> if successful.</param>
    /// <returns>True if parsing succeeded, false otherwise.</returns>
    public static bool TryParse(string version, out SemanticVersion? result)
    {
        try
        {
            result = Parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <inheritdoc/>
    public int CompareTo(SemanticVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var majorComparison = Major.CompareTo(other.Major);
        if (majorComparison != 0)
        {
            return majorComparison;
        }

        var minorComparison = Minor.CompareTo(other.Minor);
        if (minorComparison != 0)
        {
            return minorComparison;
        }

        var patchComparison = Patch.CompareTo(other.Patch);
        if (patchComparison != 0)
        {
            return patchComparison;
        }

        if (string.IsNullOrEmpty(PreRelease) && !string.IsNullOrEmpty(other.PreRelease))
        {
            return 1;
        }

        if (!string.IsNullOrEmpty(PreRelease) && string.IsNullOrEmpty(other.PreRelease))
        {
            return -1;
        }

        return string.CompareOrdinal(PreRelease, other.PreRelease);
    }

    /// <inheritdoc/>
    public int CompareTo(object? obj)
    {
        if (obj is null)
        {
            return 1;
        }

        if (obj is SemanticVersion other)
        {
            return CompareTo(other);
        }

        throw new ArgumentException($"Object must be of type {nameof(SemanticVersion)}");
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        var version = $"{Major}.{Minor}.{Patch}";
        if (!string.IsNullOrEmpty(PreRelease))
        {
            version += $"-{PreRelease}";
        }

        if (!string.IsNullOrEmpty(BuildMetadata))
        {
            version += $"+{BuildMetadata}";
        }

        return version;
    }

    [GeneratedRegex(@"^(?<major>0|[1-9]\d*)\.(?<minor>0|[1-9]\d*)\.(?<patch>0|[1-9]\d*)(?:-(?<prerelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<buildmetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$", RegexOptions.None, 1000)]
    private static partial Regex SemanticVersionRegex();
}
