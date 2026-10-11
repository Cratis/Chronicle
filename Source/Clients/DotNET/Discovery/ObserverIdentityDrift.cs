// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Observation;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Finds likely observer renames or moves from their names and kinds.
/// </summary>
internal static class ObserverIdentityDrift
{
    /// <summary>
    /// Finds newly discovered observers matching undeclared stored identities.
    /// </summary>
    /// <param name="observers">The stored observers.</param>
    /// <param name="current">The current artifacts.</param>
    /// <returns>The candidate identity changes.</returns>
    internal static IEnumerable<ArtifactIdentityDrift> Find(IEnumerable<ObserverInformation> observers, DiscoveryReport current)
    {
        var snapshot = observers.ToArray();
        var storedIds = snapshot.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);
        var declaredIds = current.Artifacts.Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);

        return current.Artifacts.Where(_ => IsObserver(_.Kind) && !storedIds.Contains(_.Id))
            .SelectMany(artifact => snapshot.Where(observer =>
                !declaredIds.Contains(observer.Id) &&
                KindOf(observer.Type) == artifact.Kind &&
                (artifact.Kind == ArtifactKind.Projection || observer.Owner != ObserverOwner.Kernel) &&
                ArtifactIdNames.CandidateNamesOf(artifact).Contains(ArtifactIdNames.NameSegmentOf(observer.Id), StringComparer.Ordinal))
                .Select(observer => new ArtifactIdentityDrift(artifact.Kind, observer.Id, artifact.Id)))
            .Distinct();
    }

    static bool IsObserver(ArtifactKind kind) => kind is ArtifactKind.Reactor or ArtifactKind.Reducer or ArtifactKind.Projection;

    static ArtifactKind? KindOf(ObserverType type) => type switch
    {
        ObserverType.Reactor => ArtifactKind.Reactor,
        ObserverType.Reducer => ArtifactKind.Reducer,
        ObserverType.Projection => ArtifactKind.Projection,
        _ => null
    };
}
