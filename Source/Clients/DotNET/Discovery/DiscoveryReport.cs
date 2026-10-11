// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Represents the artifacts found by the last discovery pass.
/// </summary>
/// <param name="HasRun">Whether discovery has completed.</param>
/// <param name="Artifacts">The discovered artifacts.</param>
public record DiscoveryReport(bool HasRun, IImmutableList<DiscoveredArtifact> Artifacts)
{
    /// <summary>
    /// The report before discovery has completed.
    /// </summary>
    public static readonly DiscoveryReport NotRun = new(false, ImmutableList<DiscoveredArtifact>.Empty);

    /// <summary>
    /// Selects artifacts of a given kind.
    /// </summary>
    /// <param name="kind">The kind to select.</param>
    /// <returns>The matching artifacts.</returns>
    public IEnumerable<DiscoveredArtifact> OfKind(ArtifactKind kind) => Artifacts.Where(_ => _.Kind == kind);

    /// <summary>
    /// Counts artifacts of a given kind.
    /// </summary>
    /// <param name="kind">The kind to count.</param>
    /// <returns>The number of matching artifacts.</returns>
    public int CountOf(ArtifactKind kind) => Artifacts.Count(_ => _.Kind == kind);

    /// <summary>
    /// Compares reports by value, including their artifact lists.
    /// </summary>
    /// <param name="other">The report to compare with.</param>
    /// <returns>Whether both reports carry the same discovery facts.</returns>
    public virtual bool Equals(DiscoveryReport? other) =>
        other is not null && HasRun == other.HasRun && Artifacts.SequenceEqual(other.Artifacts);

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hash = default(HashCode);
        hash.Add(HasRun);
        foreach (var artifact in Artifacts)
        {
            hash.Add(artifact);
        }

        return hash.ToHashCode();
    }
}
