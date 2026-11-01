// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Registrations;

/// <summary>
/// The exception thrown when not every declared artifact could produce a definition.
/// </summary>
/// <param name="unregistered">The artifacts whose definitions could not be built.</param>
public class ArtifactDefinitionsIncomplete(IEnumerable<ArtifactRegistration> unregistered)
    : Exception($"Artifact definitions could not be built for: {string.Join(", ", unregistered.Select(_ => _.ArtifactType.FullName))}")
{
    /// <summary>
    /// Gets the artifacts whose definitions could not be built.
    /// </summary>
    public IEnumerable<ArtifactRegistration> Unregistered { get; } = unregistered.ToArray();
}
