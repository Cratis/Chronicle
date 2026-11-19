// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Represents how a sink should treat the read model's last handled event sequence number when applying changes.
/// </summary>
public enum SinkWriteMode
{
    /// <summary>
    /// Apply the changes unconditionally and move the watermark forward.
    /// </summary>
    Always = 0,

    /// <summary>
    /// Apply the changes only when the incoming event sequence number is beyond the read model's
    /// <see cref="WellKnownProperties.LastHandledEventSequenceNumber"/> watermark, making a redelivery
    /// of an already applied event a no-op.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only valid for a read model whose per-document event stream is monotonic — a projection that is
    /// <c language="csharp">IsEventSourceKeyed</c>, or a reducer, both of which key every document by the event source id of the
    /// events that build it. For a projection whose key collapses several event sources onto one document
    /// (a join, a constant key or a parent hierarchy) the per-document stream is deliberately out of order, and
    /// this mode would drop legitimate events.
    /// </para>
    /// <para>
    /// A guarded write is an <b>update only</b>: it never creates the document. There is no watermark to compare
    /// against on a document that does not exist yet, and the alternative — a conditional upsert — turns the
    /// already-applied case into a duplicate key error that would discard everything queued behind it in a bulk
    /// write. Callers therefore request this mode only for a document they have just read, and write the event
    /// that creates the instance with <see cref="Always"/>. On a redelivery of that creating event the document is
    /// there, so the write is guarded and correctly does nothing.
    /// </para>
    /// </remarks>
    OnlyWhenAdvancingWatermark = 1,

    /// <summary>
    /// Insert a missing instance; otherwise apply changes only when the incoming sequence number
    /// is beyond the stored watermark, including when writes are buffered in a bulk window.
    /// </summary>
    GuardedUpsert = 2,

    /// <summary>
    /// Apply the changes with an optimistic revision check against a read model instance shared by several namespaces.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Only requested for an eligible global projection (see <c language="csharp">IProjection.SupportsFencedGlobalWrites</c>). The write succeeds only
    /// when the stored incarnation and revision are those of the read the changes were computed from, otherwise a
    /// <see cref="SinkWriteConflictException"/> is thrown and nothing is written, so the caller can read again and recompute.
    /// Redelivery is made a no-op per writing namespace, never against another namespace's sequence number.
    /// </para>
    /// <para>
    /// A sink without this concept (SQL, in-memory) ignores the mode and writes as <see cref="Always"/>.
    /// </para>
    /// </remarks>
    FencedGlobal = 3
}
