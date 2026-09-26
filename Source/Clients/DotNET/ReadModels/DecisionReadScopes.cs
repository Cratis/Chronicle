// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;

namespace Cratis.Chronicle.ReadModels;

/// <summary>Validates and merges decision scopes without exposing boundaries publicly.</summary>
internal static class DecisionReadScopes
{
    /// <summary>Validates the target of a protected read.</summary>
    /// <param name="read">The read to validate.</param>
    /// <param name="store">The expected store.</param>
    /// <param name="namespace">The expected namespace.</param>
    /// <param name="sequence">The expected sequence.</param>
    /// <exception cref="UnprotectedDecisionRead">The read is not guarded.</exception>
    /// <exception cref="DecisionReadTargetMismatch">The target differs.</exception>
    internal static void Validate(IDecisionRead read, EventStoreName store, EventStoreNamespaceName @namespace, EventSequenceId sequence)
    {
        if (!read.IsProtected)
        {
            throw new UnprotectedDecisionRead();
        }
        if (read.EventStore != store || read.Namespace != @namespace || read.EventSequenceId != sequence)
        {
            throw new DecisionReadTargetMismatch();
        }
    }

    /// <summary>Merges two reads of the same source at their earliest boundary.</summary>
    /// <param name="first">The earlier read.</param>
    /// <param name="second">The later read.</param>
    /// <returns>The merged scope.</returns>
    internal static ConcurrencyScope Merge(ConcurrencyScope first, ConcurrencyScope second)
    {
        var boundary = EventSequenceNumber.BeforeFirst;
        if (!first.SequenceNumber.IsBeforeFirst && !second.SequenceNumber.IsBeforeFirst)
        {
            boundary = first.SequenceNumber.Value < second.SequenceNumber.Value ? first.SequenceNumber : second.SequenceNumber;
        }
        var types = first.EventTypes!.Concat(second.EventTypes!).DistinctBy(_ => _.Id).ToArray();
        return first with { SequenceNumber = boundary, EventTypes = types };
    }

    /// <summary>Collects and validates detached reads for one target.</summary>
    /// <param name="reads">The reads.</param>
    /// <param name="store">The expected store.</param>
    /// <param name="namespace">The expected namespace.</param>
    /// <returns>The scopes keyed by source ID.</returns>
    internal static Dictionary<EventSourceId, ConcurrencyScope> Collect(IEnumerable<IDecisionRead> reads, EventStoreName store, EventStoreNamespaceName @namespace)
    {
        var scopes = new Dictionary<EventSourceId, ConcurrencyScope>();
        foreach (var read in reads)
        {
            Validate(read, store, @namespace, EventSequenceId.Log);
            var label = (EventSourceId)read.Key;
            scopes[label] = scopes.TryGetValue(label, out var existing) ? Merge(existing, read.Scope) : read.Scope;
        }
        return scopes;
    }
}
