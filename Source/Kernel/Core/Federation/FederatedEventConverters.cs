// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// Converts <see cref="FederatedEvent"/> to and from its wire contract.
/// </summary>
public static class FederatedEventConverters
{
    /// <summary>
    /// Convert from the contract.
    /// </summary>
    /// <param name="event">The event to convert.</param>
    /// <returns>The event.</returns>
    public static FederatedEvent ToCore(this Contracts.Federation.FederatedEvent @event) => new(
        @event.SequenceNumber,
        @event.EventType,
        @event.Generation,
        @event.EventSourceType,
        @event.EventSourceId,
        @event.EventStreamType,
        @event.EventStreamId,
        @event.Occurred,
        @event.CorrelationId,
        @event.Subject,
        @event.Content,
        [.. @event.Tags]);

    /// <summary>
    /// Convert to the contract.
    /// </summary>
    /// <param name="event">The event to convert.</param>
    /// <returns>The contract.</returns>
    public static Contracts.Federation.FederatedEvent ToContract(this FederatedEvent @event) => new()
    {
        SequenceNumber = @event.SequenceNumber,
        EventType = @event.EventType,
        Generation = @event.Generation,
        EventSourceType = @event.EventSourceType,
        EventSourceId = @event.EventSourceId,
        EventStreamType = @event.EventStreamType,
        EventStreamId = @event.EventStreamId,
        Occurred = @event.Occurred,
        CorrelationId = @event.CorrelationId,
        Subject = @event.Subject,
        Content = @event.Content,
        Tags = [.. @event.Tags]
    };
}
