// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Changes;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

public partial class Sink
{
    static bool IsWrittenThroughJoinTargets(WritePlan plan) => plan.UsesJoinTargetsOnlyFilter || plan.ShouldSuppressRootUpdateAfterRootLevelJoin;

    /// <summary>
    /// Writes a changeset whose keyed From and root join change the same property in changeset order.
    /// </summary>
    /// <remarks>
    /// Each segment is its own write. With a fence they share one transaction unless the batch is split because it is too
    /// large, and then they are committed separately, so this does not promise atomicity. Without a fence a failure between
    /// segments leaves the earlier ones written; the redelivered changeset is unguarded and rewrites every segment, so it converges.
    /// Guarded writes keep the single pass, because a stamped watermark would make a redelivery skip the later segments.
    /// </remarks>
    /// <param name="changeset">The changeset to write.</param>
    /// <param name="eventSequenceNumber">The event sequence number of the write.</param>
    /// <param name="plan">The <see cref="WritePlan"/> of the write.</param>
    /// <param name="options">The <see cref="SinkWriteOptions"/> of the write.</param>
    /// <returns>The segments to write in order, or null when the single pass applies.</returns>
    IReadOnlyList<IReadOnlyList<Change>>? OrderedSegmentsFor(
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        WritePlan plan,
        SinkWriteOptions options)
    {
        if (IsGuardedWrite(options.Mode, plan, eventSequenceNumber) || !IsOrderedByConverter(plan))
        {
            return null;
        }

        // With markers recorded the converter writes the whole changeset as one guarded update in change order.
        return MarkersFor(changeset, eventSequenceNumber, plan, options) is { RecordOwnSlot: true }
            ? null
            : ChangesetWriteOrder.SegmentsFor(changeset.Changes);
    }

    bool IsOrderedByConverter(WritePlan plan) =>
        changesetConverter is IChangesetConverterWithDirectWrites && !IsWrittenThroughJoinTargets(plan);

    async Task<IEnumerable<FailedPartition>> WriteSegments(
        Key key,
        IChangeset<AppendedEvent, ExpandoObject> changeset,
        EventSequenceNumber eventSequenceNumber,
        WritePlan plan,
        SinkWriteOptions options,
        IReadOnlyList<IReadOnlyList<Change>> segments)
    {
        var converterWithDirectWrites = (IChangesetConverterWithDirectWrites)changesetConverter;
        var markers = MarkersFor(changeset, eventSequenceNumber, plan, options);
        var failures = new List<FailedPartition>();
        foreach (var segment in segments)
        {
            var converted = await converterWithDirectWrites.ToUpdateDefinition(key, segment, changeset.InitialState, eventSequenceNumber, DirectWrites, markers);
            if (!converted.hasChanges)
            {
                continue;
            }

            var write = ScopeToExistingArrays(NewWrite(key, eventSequenceNumber, converted, plan));
            await UnsetNullParents(write, options);
            failures.AddRange(await WriteUpdate(write, changeset, options));
        }

        await RemoveChildrenFromAll(changeset);
        return failures;
    }
}
