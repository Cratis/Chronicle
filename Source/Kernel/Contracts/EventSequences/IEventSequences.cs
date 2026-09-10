// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// 16.45.x compatibility surface - do not remove.
//
// This service interface was the client contract up to and including 16.45.x and was replaced by
// the generated 17+ surface. Compiled consumers (e.g. Cratis.Stage 3.11.0's generated type
// bindings) hold assembly references to it and fail to load when it is absent. The [Service] and
// [Operation] attributes are deliberately stripped: the 18.x kernel does not serve this service,
// and the canonical descriptor set must not advertise it - the types exist for assembly-level
// compatibility only, not for the wire.
namespace Cratis.Chronicle.Contracts.EventSequences;

/// <summary>
/// Defines the contract for working with event sequences.
/// </summary>
public interface IEventSequences
{
    /// <summary>
    /// Append an event to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendResponse"/>.</returns>
    Task<AppendResponse> Append(AppendRequest request, CallContext context = default);

    /// <summary>
    /// Get the event sequences a namespace has.
    /// </summary>
    /// <param name="request">The <see cref="GetEventSequencesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetEventSequencesResponse"/>.</returns>
    Task<GetEventSequencesResponse> GetEventSequences(GetEventSequencesRequest request, CallContext context = default);

    /// <summary>
    /// Append many events to an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="AppendManyRequest"/> with all the details and events.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="AppendManyResponse"/>.</returns>
    Task<AppendManyResponse> AppendMany(AppendManyRequest request, CallContext context = default);

    /// <summary>
    /// Get the tail sequence number for an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="GetTailSequenceNumberRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The sequence number of the tail event.</returns>
    Task<GetTailSequenceNumberResponse> GetTailSequenceNumber(GetTailSequenceNumberRequest request, CallContext context = default);

    /// <summary>
    /// Get events for an event source id and specific event types.
    /// </summary>
    /// <param name="request">The <see cref="GetForEventSourceIdAndEventTypesRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="GetForEventSourceIdAndEventTypesResponse"/>.</returns>
    Task<GetForEventSourceIdAndEventTypesResponse> GetForEventSourceIdAndEventTypes(GetForEventSourceIdAndEventTypesRequest request, CallContext context = default);

    /// <summary>
    /// Check if there are events for an event source id.
    /// </summary>
    /// <param name="request"><see cref="HasEventsForEventSourceIdRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>True if it has, false if not.</returns>
    Task<HasEventsForEventSourceIdResponse> HasEventsForEventSourceId(HasEventsForEventSourceIdRequest request, CallContext context = default);

    /// <summary>
    /// Gets events from a specific event sequence number.
    /// </summary>
    /// <param name="request"><see cref="GetFromEventSequenceNumberRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>True if it has, false if not.</returns>
    Task<GetFromEventSequenceNumberResponse> GetEventsFromEventSequenceNumber(GetFromEventSequenceNumberRequest request, CallContext context = default);

    /// <summary>
    /// Query a page of events out of an event sequence, narrowed by a set of criteria.
    /// </summary>
    /// <param name="request">The <see cref="QueryEventsRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The page of matching events together with the total number of matches.</returns>
    Task<QueryEventsResponse> QueryEvents(QueryEventsRequest request, CallContext context = default);

    /// <summary>
    /// Get the number of events per time bucket in an event sequence, for driving a time range picker.
    /// </summary>
    /// <param name="request">The <see cref="GetHistogramRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The buckets containing at least one matching event, ordered by time ascending.</returns>
    Task<GetHistogramResponse> GetHistogram(GetHistogramRequest request, CallContext context = default);

    /// <summary>
    /// Revise an event in an event sequence.
    /// </summary>
    /// <param name="request">The <see cref="ReviseRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An awaitable task.</returns>
    Task Revise(ReviseRequest request, CallContext context = default);

    /// <summary>
    /// Redact a specific event by its sequence number.
    /// </summary>
    /// <param name="request">The <see cref="RedactRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>The <see cref="RedactResponse"/>.</returns>
    Task<RedactResponse> Redact(RedactRequest request, CallContext context = default);

    /// <summary>
    /// Redact all events for a specific event source.
    /// </summary>
    /// <param name="request">The <see cref="RedactForEventSourceRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>An awaitable task.</returns>
    Task RedactForEventSource(RedactForEventSourceRequest request, CallContext context = default);

    /// <summary>
    /// Complete a stream within an event sequence so that no further events can be appended to it.
    /// </summary>
    /// <param name="request">The <see cref="CompleteStreamRequest"/>.</param>
    /// <param name="context">gRPC call context.</param>
    /// <returns>A <see cref="CompleteStreamResponse"/> describing the outcome.</returns>
    Task<CompleteStreamResponse> CompleteStream(CompleteStreamRequest request, CallContext context = default);
}
