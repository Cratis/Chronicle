// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Defines the contract for working with event sequences.
/// </summary>
[Service]
public interface IEventSequences
{
    /// <summary>
    /// Append an event to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendResponse"/>.</returns>
    [Operation]
    Task<AppendResponse> Append(AppendRequest request, CallContext context = default);

    /// <summary>
    /// Append many events to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendManyRequest"/> with all the details and events.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendManyResponse"/>.</returns>
    [Operation]
    Task<AppendManyResponse> AppendMany(AppendManyRequest request, CallContext context = default);

    /// <summary>
    /// Get the tail sequence number for an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="GetTailSequenceNumberRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The sequence number of the tail event.</returns>
    [Operation]
    Task<GetTailSequenceNumberResponse> GetTailSequenceNumber(GetTailSequenceNumberRequest request, CallContext context = default);

    /// <summary>
    /// Get events for an event source id and specific event types.
    /// </summary>
    /// <param name="request">The <see cref="GetForEventSourceIdAndEventTypesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetForEventSourceIdAndEventTypesResponse"/>.</returns>
    [Operation]
    Task<GetForEventSourceIdAndEventTypesResponse> GetForEventSourceIdAndEventTypes(GetForEventSourceIdAndEventTypesRequest request, CallContext context = default);

    /// <summary>
    /// Check if there are events for an event source id.
    /// </summary>
    /// <param name="request"><see cref="HasEventsForEventSourceIdRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>True if it has, false if not.</returns>
    [Operation]
    Task<HasEventsForEventSourceIdResponse> HasEventsForEventSourceId(HasEventsForEventSourceIdRequest request, CallContext context = default);

    /// <summary>
    /// Gets events from a specific event sequence number.
    /// </summary>
    /// <param name="request"><see cref="GetFromEventSequenceNumberRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>True if it has, false if not.</returns>
    [Operation]
    Task<GetFromEventSequenceNumberResponse> GetEventsFromEventSequenceNumber(GetFromEventSequenceNumberRequest request, CallContext context = default);

    /// <summary>
    /// Compensate an event in an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="CompensateRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An awaitable task.</returns>
    [Operation]
    Task Compensate(CompensateRequest request, CallContext context = default);
}
