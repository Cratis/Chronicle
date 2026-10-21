// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Compares the software versions projection definitions were registered with.
/// </summary>
internal static class ProjectionVersions
{
    /// <summary>
    /// Gets whether a version is older than another, by semantic version precedence. Build metadata is ignored.
    /// </summary>
    /// <param name="candidate">The version to check, typically the one on an incoming registration.</param>
    /// <param name="reference">The version to check against, typically the one stored with a definition.</param>
    /// <returns><see langword="true"/> only when both versions are valid semantic versions and <paramref name="candidate"/> has lower precedence.</returns>
    /// <remarks>
    /// A missing or unparseable version on either side is never older, so such registrations keep behaving as they
    /// did before versions existed.
    /// </remarks>
    internal static bool IsOlder(string? candidate, string? reference)
    {
        if (string.IsNullOrWhiteSpace(candidate) || string.IsNullOrWhiteSpace(reference)) return false;
        if (!SemanticVersion.TryParse(candidate, out var candidateVersion) || !SemanticVersion.TryParse(reference, out var referenceVersion)) return false;

        return candidateVersion! < referenceVersion!;
    }
}
