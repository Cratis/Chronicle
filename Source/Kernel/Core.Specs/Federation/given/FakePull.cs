// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.Federation.given;

/// <summary>
/// A fake producer: answers pulls from a queue, or from a custom function.
/// </summary>
public class FakePull : IFederationPull
{
    readonly Queue<FederatedEventBatchResponse> _responses = new();

    public List<PullRequest> Requests { get; } = [];

    public Func<PullRequest, Task<QueryResult<FederatedEventBatchResponse>>>? Handler { get; set; }

    public static FederatedEventBatchResponse Batch(string origin, ulong next, ulong tail, params Contracts.Federation.FederatedEvent[] events) => new()
    {
        Status = FederatedEventBatchStatus.Ok,
        OriginEventStore = origin,
        NextSequenceNumber = next,
        TailSequenceNumber = tail,
        Events = events
    };

    public static FederatedEventBatchResponse WithStatus(string status) => new() { Status = status, OriginEventStore = "remote" };

    public static Contracts.Federation.FederatedEvent Event(ulong sequenceNumber, string eventType = "type-a") => new()
    {
        SequenceNumber = sequenceNumber,
        EventType = eventType,
        Generation = 1,
        EventSourceType = "Default",
        EventSourceId = $"source-{sequenceNumber}",
        EventStreamType = "All",
        EventStreamId = "Default",
        Occurred = DateTimeOffset.UnixEpoch,
        CorrelationId = Guid.NewGuid().ToString(),
        Subject = string.Empty,
        Content = "{}"
    };

    public void Enqueue(FederatedEventBatchResponse response) => _responses.Enqueue(response);

    public Task<QueryResult<FederatedEventBatchResponse>> Pull(PullRequest request, CallContext callContext = default)
    {
        Requests.Add(request);
        if (Handler is not null)
        {
            return Handler(request);
        }

        var response = _responses.Count > 0 ? _responses.Dequeue() : Batch("remote", request.FromSequenceNumber, request.FromSequenceNumber);
        return Task.FromResult(QueryResult<FederatedEventBatchResponse>.Success(Guid.NewGuid(), response));
    }
}
