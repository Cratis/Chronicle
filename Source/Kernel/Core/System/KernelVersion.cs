// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Sys;

/// <summary>
/// Provides the version stamped on the running kernel executable, not the Core library.
/// </summary>
internal static class KernelVersion
{
    /// <summary>
    /// Gets the informational version of the running kernel executable, including build metadata.
    /// </summary>
    internal static readonly string InformationalVersion = ReadInformationalVersion();

    /// <summary>
    /// Gets the running kernel version without build metadata.
    /// </summary>
    internal static SemanticVersion Current { get; } = Parse(InformationalVersion);

    /// <summary>
    /// Parses an informational version into a semantic version, ignoring build metadata.
    /// </summary>
    /// <param name="informationalVersion">The informational version to parse.</param>
    /// <returns>The parsed version, or <see cref="SemanticVersion.NotSet"/> when it cannot be parsed.</returns>
    internal static SemanticVersion Parse(string informationalVersion) =>
        SemanticVersion.TryParse(ParseVersion(informationalVersion), out var version) ? version! : SemanticVersion.NotSet;

    /// <summary>
    /// Determines whether a version identifies a released kernel rather than a development placeholder.
    /// </summary>
    /// <param name="version">The version to check.</param>
    /// <returns>True when the version is neither unset nor the 1.0.0 development placeholder.</returns>
    internal static bool IsKnown(SemanticVersion version) =>
        version.CompareTo(SemanticVersion.NotSet) != 0 && version.CompareTo(new SemanticVersion(1, 0, 0)) != 0;

    /// <summary>
    /// Removes build metadata from an informational version.
    /// </summary>
    /// <param name="informationalVersion">The informational version.</param>
    /// <returns>The version without build metadata.</returns>
    internal static string ParseVersion(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return plusIndex > 0 ? informationalVersion[..plusIndex] : informationalVersion;
    }

    /// <summary>
    /// Gets the commit identifier from the build metadata of an informational version.
    /// </summary>
    /// <param name="informationalVersion">The informational version.</param>
    /// <returns>The commit identifier, or an empty string when there is none.</returns>
    internal static string ParseCommitSha(string informationalVersion)
    {
        var plusIndex = informationalVersion.IndexOf('+', StringComparison.Ordinal);
        return plusIndex >= 0 && plusIndex < informationalVersion.Length - 1
            ? informationalVersion[(plusIndex + 1)..]
            : string.Empty;
    }

    static string ReadInformationalVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? typeof(KernelVersion).Assembly;
        return assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";
    }
}
