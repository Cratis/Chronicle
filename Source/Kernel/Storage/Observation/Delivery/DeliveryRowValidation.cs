// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Checks delivery row ranges and ownership before storage.
/// </summary>
static class DeliveryRowValidation
{
    /// <summary>
    /// Validates the root and all nested lane and envelope values.
    /// </summary>
    /// <param name="root">The root to check.</param>
    /// <exception cref="InvalidDeliveryCommit">The commit token is empty.</exception>
    public static void Root(ObserverDeliveryRoot root)
    {
        _ = DeliveryRanges.Bounded(root.Format.Value, nameof(root.Format), 1);
        _ = DeliveryRanges.Counter(root.Generation.Value, nameof(root.Generation));
        _ = DeliveryRanges.Bounded(root.KeyDefinitionVersion, nameof(root.KeyDefinitionVersion));
        _ = DeliveryRanges.Bounded(root.MapVersion, nameof(root.MapVersion));
        _ = DeliveryRanges.Bounded(root.Lanes.Length, nameof(root.Lanes), 1, DeliveryRanges.MaximumLanes);
        _ = DeliveryRanges.Counter(root.ActivationEpoch.Value, nameof(root.ActivationEpoch));
        _ = DeliveryRanges.Clock(root.ScheduleClock, nameof(root.ScheduleClock));
        _ = DeliveryRanges.Counter(root.ScheduleTicket.Value, nameof(root.ScheduleTicket));
        _ = DeliveryRanges.Counter(root.Revision.Value, nameof(root.Revision));
        if (root.LastCommit.Value == Guid.Empty)
        {
            throw new InvalidDeliveryCommit("A new, non-empty commit token is required.");
        }

        foreach (var lane in root.Lanes)
        {
            _ = DeliveryRanges.Position(lane.U, nameof(lane.U));
            _ = DeliveryRanges.Position(lane.W, nameof(lane.W));
            _ = DeliveryRanges.Position(lane.NextBatchEnd, nameof(lane.NextBatchEnd));
            _ = DeliveryRanges.Counter(lane.Attempts, nameof(lane.Attempts));
        }

        Envelope(root.CoverageHead);
        Envelope(root.Operation);
    }

    /// <summary>
    /// Validates the observer and row revision against the committing root.
    /// </summary>
    /// <param name="root">The committing root.</param>
    /// <param name="observer">The row observer.</param>
    /// <param name="revision">The row revision.</param>
    /// <exception cref="InvalidDeliveryCommit">The observer or revision differs.</exception>
    public static void Ownership(ObserverDeliveryRoot root, ObserverId observer, DeliveryRevision revision)
    {
        _ = DeliveryRanges.Counter(revision.Value, nameof(revision));
        if (observer != root.Observer || revision != root.Revision)
        {
            throw new InvalidDeliveryCommit("The row observer and revision must match the root.");
        }
    }

    /// <summary>
    /// Validates an F row.
    /// </summary>
    /// <param name="row">The parked key.</param>
    public static void ParkedKey(ParkedKey row)
    {
        Key(row.Key);
        _ = DeliveryRanges.Position(row.Next, nameof(row.Next));
        _ = DeliveryRanges.Bounded(row.Attempts, nameof(row.Attempts));
        _ = DeliveryRanges.Clock(row.RetryDue, nameof(row.RetryDue));
        _ = DeliveryRanges.Counter(row.ReadyTicket.Value, nameof(row.ReadyTicket));
    }

    /// <summary>
    /// Validates an S row, including its non-empty half-open interval.
    /// </summary>
    /// <param name="row">The skip range.</param>
    /// <exception cref="InvalidDeliveryCommit">The range is empty or inverted.</exception>
    public static void SkipRange(SkipRange row)
    {
        Key(row.Key);
        _ = DeliveryRanges.Position(row.From, nameof(row.From));
        _ = DeliveryRanges.Position(row.To, nameof(row.To));
        if (row.From.Value >= row.To.Value)
        {
            throw new InvalidDeliveryCommit("An S range must have From less than To.");
        }
    }

    static void Envelope(VersionedEnvelope? envelope)
    {
        if (envelope is null)
        {
            return;
        }

        _ = DeliveryRanges.Bounded(envelope.SchemaVersion, nameof(envelope.SchemaVersion));
        _ = DeliveryRanges.Bounded(envelope.Payload.Length, nameof(envelope.Payload), 0, DeliveryRanges.MaximumPayload);
    }

    static void Key(SequencingKeyIdentity key)
    {
        _ = DeliveryRanges.Bounded((int)key.Kind, nameof(key.Kind), 0, 2);
        _ = DeliveryRanges.Bounded(key.Slice.Value, nameof(key.Slice), 0, SequencingKeySlice.Count - 1);
        _ = DeliveryRanges.Bounded(key.Key.Span.Length, nameof(key.Key), 1, CanonicalSequencingKey.MaximumLength);
        _ = SequencingKeyIdentity.FromStored(key.Kind, key.Hash, key.Key, key.Slice);
    }
}
