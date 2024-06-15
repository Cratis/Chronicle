// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Events;
using Cratis.Identities;
using Orleans.Streams;

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Extension methods for <see cref="StreamSequenceToken"/>.
/// </summary>
public static class StreamSequenceTokenExtensions
{
    /// <summary>
    /// Check if a token represents a warm up event.
    /// </summary>
    /// <param name="token"><see cref="StreamSequenceToken"/> to check.</param>
    /// <returns>True if it is a warm up event, false if not.</returns>
    public static bool IsWarmUp(this StreamSequenceToken token) => token.SequenceNumber == (long)EventSequenceNumber.WarmUp.Value;

    /// <summary>
    /// WarmUp the stream.
    /// </summary>
    /// <param name="stream">Stream to warm up.</param>
    /// <returns>Awaitable task.</returns>
    public static async Task WarmUp(this IAsyncStream<AppendedEvent> stream)
    {
        var @event = new AppendedEvent(
            new(EventSequenceNumber.WarmUp, new EventType(EventTypeId.Unknown, 1)),
            new(
                EventSourceId.Unspecified,
                EventSequenceNumber.WarmUp,
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow,
                EventStoreName.NotSet,
                EventStoreNamespaceName.NotSet,
                CorrelationId.New(),
                [],
                Identity.System,
                EventObservationState.Initial),
            new ExpandoObject());
        await stream!.OnNextAsync(@event, new EventSequenceNumberToken());
    }
}
