// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Grpc;
using Cratis.Chronicle.Storage;

namespace Cratis.Chronicle.Sequences;

/// <summary>
/// Represents whether a scope is covered by a persisted closure.
/// </summary>
/// <param name="IsCompleted">Whether the scope is closed.</param>
[ReadModel]
[BelongsTo(WellKnownServices.EventSequences)]
public record StreamScopeCompletion(bool IsCompleted)
{
    /// <summary>
    /// Inspect completion of a scope.
    /// </summary>
    /// <param name="storage">The storage.</param>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The namespace.</param>
    /// <param name="eventSequenceId">The event sequence.</param>
    /// <param name="eventSourceId">The optional event source identifier.</param>
    /// <param name="eventSourceType">The optional event source type.</param>
    /// <param name="eventStreamType">The optional stream type.</param>
    /// <param name="eventStreamId">The optional stream identifier.</param>
    /// <returns>The scope completion state.</returns>
    public static async Task<StreamScopeCompletion> IsStreamScopeCompleted(
        IStorage storage,
        EventStoreName eventStore,
        EventStoreNamespaceName @namespace,
        EventSequenceId eventSequenceId,
        string? eventSourceId = default,
        string? eventSourceType = default,
        string? eventStreamType = default,
        string? eventStreamId = default)
    {
        var closures = storage.GetEventStore(eventStore).GetNamespace(@namespace).GetClosedStreamsConstraints(eventSequenceId);
        var scope = ClosedStreamConverters.ToScope(eventSourceId, eventSourceType, eventStreamType, eventStreamId);

        return new((await closures.GetCovering(scope, await closures.GetDimensionsInUse())).Any());
    }
}
