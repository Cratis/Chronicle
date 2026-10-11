// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Events;
using Cratis.Chronicle.Contracts.EventTypes;

namespace Cratis.Chronicle.Discovery;

/// <summary>
/// Finds likely event type renames by comparing unique schema shapes.
/// </summary>
internal static class EventTypeIdentityDrift
{
    /// <summary>
    /// Finds new event identities that uniquely match an undeclared stored event.
    /// </summary>
    /// <param name="stored">The stored event types.</param>
    /// <param name="current">The current artifacts.</param>
    /// <param name="schemaFor">Gets the current schema for an identity.</param>
    /// <returns>The likely identity changes.</returns>
    internal static IEnumerable<ArtifactIdentityDrift> Find(IEnumerable<EventTypeDetailsResponse> stored, DiscoveryReport current, Func<string, string> schemaFor)
    {
        var snapshot = stored.ToArray();
        var storedIds = snapshot.Select(_ => _.Type.Id).ToHashSet(StringComparer.Ordinal);
        var declaredIds = current.OfKind(ArtifactKind.EventType).Select(_ => _.Id).ToHashSet(StringComparer.Ordinal);
        var candidates = snapshot.Where(_ => _.Owner == EventTypeOwner.Client && !declaredIds.Contains(_.Type.Id))
            .Select(_ => (_.Type.Id, Shape: EventSchemaShape.Of(_.Schema))).Where(_ => _.Shape is not null).ToArray();

        return current.OfKind(ArtifactKind.EventType).Where(_ => !storedIds.Contains(_.Id))
            .SelectMany(artifact => MatchesFor(artifact, candidates, schemaFor));
    }

    static IEnumerable<ArtifactIdentityDrift> MatchesFor(DiscoveredArtifact artifact, (string Id, string? Shape)[] candidates, Func<string, string> schemaFor)
    {
        var shape = EventSchemaShape.Of(schemaFor(artifact.Id));
        var matches = candidates.Where(_ => shape is not null && _.Shape == shape).Select(_ => _.Id).Distinct().ToArray();

        return matches.Length == 1 ? [new(ArtifactKind.EventType, matches[0], artifact.Id)] : [];
    }
}
