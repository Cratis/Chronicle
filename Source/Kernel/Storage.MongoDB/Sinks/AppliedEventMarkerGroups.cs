// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.Sinks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Collects, in change order, the writes of a changeset that are guarded by an applied event marker slot each.
/// </summary>
/// <param name="context">The <see cref="AppliedEventMarkerContext"/> of the event.</param>
/// <param name="ownSlot">The path of the slot the write of the key document records.</param>
internal sealed class AppliedEventMarkerGroups(AppliedEventMarkerContext context, string ownSlot)
{
    readonly List<AppliedEventMarkerGroup> _groups = [];
    AppliedEventMarkerSlot OwnMarker => new(ownSlot, context.OwnStream, (long)context.SequenceNumber.Value);

    /// <summary>
    /// Closes the write of the key document that has been built so far as a group.
    /// </summary>
    /// <param name="update">The update built so far; reset to nothing when it was closed.</param>
    public void CloseOwn(ref UpdateDefinition<BsonDocument>? update)
    {
        if (update is null)
        {
            return;
        }

        _groups.Add(new(update, OwnMarker));
        update = null;
    }

    /// <summary>
    /// Adds the write of a resolved join whose source is an appended event.
    /// </summary>
    /// <param name="update">The update the join makes, or null when it only records its slot.</param>
    /// <param name="resolved">The resolved join.</param>
    /// <param name="source">The event the join was resolved from.</param>
    /// <param name="target">The property and key the join is on.</param>
    public void AddResolvedJoin(UpdateDefinition<BsonDocument>? update, ResolvedJoin resolved, AppendedEvent source, JoinFilterTarget target)
    {
        var definitionId = AppliedEventMarkers.DefinitionId(source.Context.EventType.Id.Value, resolved.OnProperty.Path);
        var stream = AppliedEventMarkers.StreamHash(source.Context.Namespace.Value, context.EventSequence, source.Context.EventSourceId.Value);
        _groups.Add(new(update, new(AppliedEventMarkerSlots.Join(definitionId), stream, (long)source.Context.SequenceNumber.Value), target));
    }

    /// <summary>
    /// Builds the guarded update of everything collected.
    /// </summary>
    /// <returns>The update.</returns>
    public UpdateDefinition<BsonDocument> ToUpdate()
    {
        var groups = _groups.Exists(_ => !_.IsResolvedJoin)
            ? _groups
            : [new AppliedEventMarkerGroup(null, OwnMarker), .. _groups];
        return AppliedEventMarkerUpdate.Build(groups);
    }
}
