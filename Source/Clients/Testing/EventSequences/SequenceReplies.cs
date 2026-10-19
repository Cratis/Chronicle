// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Primitives;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Sequences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Testing.EventSequences;

/// <summary>
/// Drops replies only for successful receipted appends. Every other sequence operation is unchanged.
/// </summary>
/// <param name="inner">The real service.</param>
/// <param name="replies">The reply-loss driver.</param>
internal sealed class SequenceReplies(IEventSequences inner, DropReplyInterceptor replies) : IEventSequences
{
    /// <inheritdoc/>
    public async Task<CommandResult<AppendResponse>> Append(AppendRequest request, CallContext callContext = default)
    {
        var result = await inner.Append(request, callContext);
        return replies.After(DroppedReply.Append, result, request.EffectReceipt is not null && result.IsSuccess && result.Response.IsSuccess);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<AppendResponse>> AppendWithNamedTags(AppendWithNamedTagsRequest request, CallContext callContext = default)
    {
        var result = await inner.AppendWithNamedTags(request, callContext);
        return replies.After(DroppedReply.Append, result, request.EffectReceipt is not null && result.IsSuccess && result.Response.IsSuccess);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<AppendManyResponse>> AppendMany(AppendManyRequest request, CallContext callContext = default)
    {
        var result = await inner.AppendMany(request, callContext);
        return replies.After(DroppedReply.Append, result, request.EffectReceipt is not null && result.IsSuccess && result.Response.IsSuccess);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<AppendManyResponse>> AppendManyWithNamedTags(AppendManyWithNamedTagsRequest request, CallContext callContext = default)
    {
        var result = await inner.AppendManyWithNamedTags(request, callContext);
        return replies.After(DroppedReply.Append, result, request.EffectReceipt is not null && result.IsSuccess && result.Response.IsSuccess);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<AppendManyResponse>> AppendManyForEventSources(AppendManyForEventSourcesRequest request, CallContext callContext = default)
    {
        var result = await inner.AppendManyForEventSources(request, callContext);
        return replies.After(DroppedReply.Append, result, request.EffectReceipt is not null && result.IsSuccess && result.Response.IsSuccess);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<AppendManyResponse>> AppendManyForEventSourcesWithNamedTags(AppendManyForEventSourcesWithNamedTagsRequest request, CallContext callContext = default)
    {
        var result = await inner.AppendManyForEventSourcesWithNamedTags(request, callContext);
        return replies.After(DroppedReply.Append, result, request.EffectReceipt is not null && result.IsSuccess && result.Response.IsSuccess);
    }

    /// <inheritdoc/>
    public Task<CommandResult<AppendOnceResponse>> AppendOnce(AppendOnceRequest request, CallContext callContext = default) => inner.AppendOnce(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<AppendOnceResponse>> AppendOnceWithNamedTags(AppendOnceWithNamedTagsRequest request, CallContext callContext = default) => inner.AppendOnceWithNamedTags(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<AppendManyOnceResponse>> AppendManyOnce(AppendManyOnceRequest request, CallContext callContext = default) => inner.AppendManyOnce(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<AppendManyOnceResponse>> AppendManyOnceWithNamedTags(AppendManyOnceWithNamedTagsRequest request, CallContext callContext = default) => inner.AppendManyOnceWithNamedTags(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<CompleteStreamResponse>> CompleteStream(CompleteStreamRequest request, CallContext callContext = default) => inner.CompleteStream(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<CompleteStreamResponse>> CompleteStreamScope(CompleteStreamScopeRequest request, CallContext callContext = default) => inner.CompleteStreamScope(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult> Redact(RedactRequest request, CallContext callContext = default) => inner.Redact(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult> RedactForEventSource(RedactForEventSourceRequest request, CallContext callContext = default) => inner.RedactForEventSource(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<ReopenStreamScopeResponse>> ReopenStreamScope(ReopenStreamScopeRequest request, CallContext callContext = default) => inner.ReopenStreamScope(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult> Revise(ReviseRequest request, CallContext callContext = default) => inner.Revise(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<VerifyContentResponse>> VerifyContent(VerifyContentRequest request, CallContext callContext = default) => inner.VerifyContent(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<AppendedEventResponse>>> QueryEvents(QueryEventsRequest request, CallContext callContext = default) => inner.QueryEvents(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<AppendedEventResponse>>> QueryEventsWithNamedTags(QueryEventsWithNamedTagsRequest request, CallContext callContext = default) => inner.QueryEventsWithNamedTags(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<AppendedEventResponse>>> AppendedEvents(AppendedEventsRequest request, CallContext callContext = default) => inner.AppendedEvents(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<AppendedEventResponse>>> ForEventSourceIdAndEventTypes(ForEventSourceIdAndEventTypesRequest request, CallContext callContext = default) => inner.ForEventSourceIdAndEventTypes(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<AppendedEventResponse>>> FromSequenceNumber(FromSequenceNumberRequest request, CallContext callContext = default) => inner.FromSequenceNumber(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<ClosedStreamResponse>>> ClosedStreams(ClosedStreamsRequest request, CallContext callContext = default) => inner.ClosedStreams(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<EventMetadataResponse>>> MetadataAt(MetadataAtRequest request, CallContext callContext = default) => inner.MetadataAt(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<EventSequenceNamesResponse>>> AllEventSequences(AllEventSequencesRequest request, CallContext callContext = default) => inner.AllEventSequences(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<EventSequenceTailResponse>> TailSequenceNumber(TailSequenceNumberRequest request, CallContext callContext = default) => inner.TailSequenceNumber(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<EventSourceEventsResponse>> HasEventsForEventSourceId(HasEventsForEventSourceIdRequest request, CallContext callContext = default) => inner.HasEventsForEventSourceId(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<ExportedEventResponse>>> ExportEvents(ExportEventsRequest request, CallContext callContext = default) => inner.ExportEvents(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<SequenceHistogramBucketResponse>>> SequenceHistogram(SequenceHistogramRequest request, CallContext callContext = default) => inner.SequenceHistogram(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<IEnumerable<SequenceHistogramBucketResponse>>> SequenceHistogramWithNamedTags(SequenceHistogramWithNamedTagsRequest request, CallContext callContext = default) => inner.SequenceHistogramWithNamedTags(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<Contracts.Storage.EventSequences.HistogramResolution>> ParseResolution(ParseResolutionRequest request, CallContext callContext = default) => inner.ParseResolution(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<SerializableDateTimeOffset>> EndOf(EndOfRequest request, CallContext callContext = default) => inner.EndOf(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<StreamScopeCompletionResponse>> IsStreamScopeCompleted(IsStreamScopeCompletedRequest request, CallContext callContext = default) => inner.IsStreamScopeCompleted(request, callContext);
}
