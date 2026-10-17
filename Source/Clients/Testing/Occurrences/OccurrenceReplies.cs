// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Contracts.Occurrences;
using Cratis.Chronicle.Contracts.Queries;
using Cratis.Chronicle.Contracts.Storage.Occurrences;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Testing.Occurrences;

/// <summary>
/// Decorates only occurrence mutations; reads and schema registration pass through unchanged.
/// </summary>
/// <param name="inner">The real service.</param>
/// <param name="replies">The reply-loss driver.</param>
internal sealed class OccurrenceReplies(IOccurrences inner, DropReplyInterceptor replies) : IOccurrences
{
    /// <inheritdoc/>
    public async Task<CommandResult<ClaimOccurrencesResponse>> ClaimOccurrences(ClaimOccurrencesRequest request, CallContext callContext = default)
    {
        var result = await inner.ClaimOccurrences(request, callContext);
        return replies.After(DroppedReply.ClaimOccurrences, result, result.IsSuccess && result.Response?.Claims.Any() == true);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<CompleteOccurrenceResponse>> CompleteOccurrence(CompleteOccurrenceRequest request, CallContext callContext = default)
    {
        var result = await inner.CompleteOccurrence(request, callContext);
        return replies.After(DroppedReply.CompleteOccurrence, result, result.IsSuccess && result.Response?.Status is OccurrenceLeaseResult.Updated or OccurrenceLeaseResult.AlreadyCompleted);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<CompleteOccurrenceResponse>> FailOccurrence(FailOccurrenceRequest request, CallContext callContext = default)
    {
        var result = await inner.FailOccurrence(request, callContext);
        return replies.After(DroppedReply.FailOccurrence, result, result.IsSuccess && result.Response?.Status == OccurrenceLeaseResult.Updated);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<RecordEffectReceiptResponse>> RecordEffectReceipt(RecordEffectReceiptRequest request, CallContext callContext = default)
    {
        var result = await inner.RecordEffectReceipt(request, callContext);
        return replies.After(DroppedReply.RecordEffectReceipt, result, result.IsSuccess);
    }

    /// <inheritdoc/>
    public async Task<CommandResult<RecordOccurrenceResponse>> RecordOccurrence(RecordOccurrenceRequest request, CallContext callContext = default)
    {
        var result = await inner.RecordOccurrence(request, callContext);
        return replies.After(DroppedReply.RecordOccurrence, result, result.IsSuccess && result.Response?.Status is OccurrenceIngressStatus.Recorded or OccurrenceIngressStatus.AlreadyRecorded);
    }

    /// <inheritdoc/>
    public Task<CommandResult<RecordLifecycleOccurrenceResponse>> RecordLifecycleOccurrence(RecordLifecycleOccurrenceRequest request, CallContext callContext = default) => inner.RecordLifecycleOccurrence(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<RegisterTriggersResponse>> RegisterTriggers(RegisterTriggersRequest request, CallContext callContext = default) => inner.RegisterTriggers(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<CompleteOccurrenceResponse>> RenewOccurrenceLease(RenewOccurrenceLeaseRequest request, CallContext callContext = default) => inner.RenewOccurrenceLease(request, callContext);

    /// <inheritdoc/>
    public Task<CommandResult<bool>> RetryQuarantinedOccurrence(RetryQuarantinedOccurrenceRequest request, CallContext callContext = default) => inner.RetryQuarantinedOccurrence(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<EffectReceiptRecordResponse?>> GetEffectReceipt(GetEffectReceiptRequest request, CallContext callContext = default) => inner.GetEffectReceipt(request, callContext);

    /// <inheritdoc/>
    public Task<QueryResult<OccurrenceSnapshotResponse?>> GetOccurrence(GetOccurrenceRequest request, CallContext callContext = default) => inner.GetOccurrence(request, callContext);

    /// <inheritdoc/>
    public IObservable<QueryResult<IEnumerable<OccurrenceSnapshotResponse>>> ObserveOccurrences(ObserveOccurrencesRequest request, CallContext callContext = default) => inner.ObserveOccurrences(request, callContext);
}
