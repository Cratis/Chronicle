// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ChoreographyVersion = Cratis.Choreography.Patching.SemanticVersion;
using ChronicleVersion = Cratis.Chronicle.Concepts.System.SemanticVersion;

namespace Cratis.Chronicle.Patching;

/// <summary>
/// Converts between Chronicle's version type, which is stored and sent between kernels, and the one patching in Cratis Choreography works with.
/// </summary>
internal static class PatchVersions
{
    /// <summary>
    /// Converts a Chronicle version to the patching version.
    /// </summary>
    /// <param name="version">The version to convert.</param>
    /// <returns>The same version as a patching version.</returns>
    internal static ChoreographyVersion ToPatching(this ChronicleVersion version) =>
        new(version.Major, version.Minor, version.Patch, version.PreRelease, version.BuildMetadata);

    /// <summary>
    /// Converts a patching version to a Chronicle version.
    /// </summary>
    /// <param name="version">The version to convert.</param>
    /// <returns>The same version as a Chronicle version.</returns>
    internal static ChronicleVersion ToChronicle(this ChoreographyVersion version) =>
        new(version.Major, version.Minor, version.Patch, version.PreRelease, version.BuildMetadata);
}
