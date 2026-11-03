// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Collects the slots of the joins a changeset resolved into the key document.
/// </summary>
internal sealed class AppliedEventMarkerTracker
{
    readonly List<AppliedResolvedJoinSlot> _slots = [];

    /// <summary>
    /// Gets the collected slots.
    /// </summary>
    public IReadOnlyList<AppliedResolvedJoinSlot> ResolvedJoins => _slots;

    /// <summary>
    /// Collects the slot of a resolved join whose source is an appended event.
    /// </summary>
    /// <param name="resolved">The resolved join.</param>
    public void Add(ResolvedJoin resolved)
    {
        if (resolved.Source is not AppendedEvent source)
        {
            return;
        }

        var definitionId = AppliedEventMarkers.DefinitionId(source.Context.EventType.Id.Value, resolved.OnProperty.Path);
        var stream = AppliedEventMarkers.StreamHash(source.Context.Namespace.Value, source.Context.EventSourceId.Value);
        _slots.Add(new(definitionId, stream, source.Context.SequenceNumber));
    }
}
