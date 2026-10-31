// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation.Sequencing;

namespace Cratis.Chronicle.Storage.Observation.Delivery;

/// <summary>
/// Validates the immutable commit before a provider performs any I/O.
/// </summary>
public static class DeliveryCommitValidator
{
    /// <summary>
    /// Checks root CAS, row ownership, revisions, ranges and per-namespace operation invariants.
    /// </summary>
    /// <param name="commit">The immutable commit.</param>
    /// <exception cref="InvalidDeliveryCommit">A commit invariant is violated; nothing is written.</exception>
    /// <exception cref="DeliveryValueOutOfRange">A value is out of range; nothing is written.</exception>
    /// <exception cref="DeliveryRevisionExhausted">The expected revision cannot advance.</exception>
    public static void Validate(DeliveryCommit commit)
    {
        DeliveryRowValidation.Root(commit.Root);
        if (commit.Root.Revision != commit.Expected.Next())
        {
            throw new InvalidDeliveryCommit("The root revision must immediately follow the expected revision.");
        }

        ValidateParkedKeys(commit);
        ValidateSkipRanges(commit);
    }

    static void ValidateParkedKeys(DeliveryCommit commit)
    {
        var identities = new HashSet<(DeliveryGeneration, SequencingKeyHash)>();
        var tickets = new HashSet<DeliveryTicket>();
        foreach (var operation in commit.ParkedKeys)
        {
            _ = DeliveryRanges.Counter(operation.Generation.Value, nameof(operation.Generation));
            if (!identities.Add((operation.Generation, operation.KeyHash)))
            {
                throw new InvalidDeliveryCommit("An F identity has more than one operation.");
            }

            if (operation.Row is not { } row)
            {
                continue;
            }

            DeliveryRowValidation.Ownership(commit.Root, row.Observer, row.Revision);
            DeliveryRowValidation.ParkedKey(row);
            if (!tickets.Add(row.ReadyTicket))
            {
                throw new InvalidDeliveryCommit("Two F rows share a ready ticket.");
            }
        }
    }

    static void ValidateSkipRanges(DeliveryCommit commit)
    {
        var identities = new HashSet<(DeliveryGeneration, SequencingKeyHash, ulong)>();
        var inserted = new List<SkipRange>();
        foreach (var operation in commit.SkipRanges)
        {
            _ = DeliveryRanges.Counter(operation.Generation.Value, nameof(operation.Generation));
            _ = DeliveryRanges.Position(operation.From, nameof(operation.From));
            if (!identities.Add((operation.Generation, operation.KeyHash, operation.From.Value)))
            {
                throw new InvalidDeliveryCommit("An S identity has more than one operation.");
            }

            if (operation.Row is not { } row)
            {
                continue;
            }

            DeliveryRowValidation.Ownership(commit.Root, row.Observer, row.Revision);
            DeliveryRowValidation.SkipRange(row);
            inserted.Add(row);
        }

        ValidateDisjointInserts(inserted);
    }

    static void ValidateDisjointInserts(List<SkipRange> inserted)
    {
        foreach (var group in inserted.GroupBy(row => (row.Generation, row.Key.Hash)))
        {
            SkipRange? previous = null;
            foreach (var row in group.OrderBy(row => row.From.Value))
            {
                if (previous is not null && row.From.Value < previous.To.Value)
                {
                    throw new InvalidDeliveryCommit("Inserted S ranges for a key overlap.");
                }

                previous = row;
            }
        }
    }
}
