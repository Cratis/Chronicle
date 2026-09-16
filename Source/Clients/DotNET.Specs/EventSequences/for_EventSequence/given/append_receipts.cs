// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventSequences;
using Cratis.Chronicle.Contracts.Sequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public static class append_receipts
{
    public static AppendResponse Complete(AppendResponse response, AppendRequest request)
    {
        response.Receipt = Create(response.SequenceNumber, request.EventSourceId, request.EventType, request.EventStore, request.Namespace);
        return response;
    }

    public static AppendManyResponse Complete(AppendManyResponse response, AppendManyRequest request)
    {
        response.Receipts = request.Events.Select((@event, index) => Create(response.SequenceNumbers.ElementAt(index), request.EventSourceId, @event.EventType, request.EventStore, request.Namespace)).ToArray();
        return response;
    }

    public static AppendManyResponse Complete(AppendManyResponse response, AppendManyForEventSourcesRequest request)
    {
        response.Receipts = request.Events.Select((@event, index) => Create(response.SequenceNumbers.ElementAt(index), @event.EventSourceId, @event.EventType, request.EventStore, request.Namespace, @event.EventSourceType, @event.EventStreamType, @event.EventStreamId)).ToArray();
        return response;
    }

    public static AppendReceipt Create(ulong number, string source, Contracts.Sequences.EventType type, string store, string @namespace, string sourceType = "", string streamType = "", string streamId = "") => new()
    {
        SequenceNumber = number,
        EventSourceId = source,
        EventTypeId = type.Id,
        Generation = type.Generation,
        Tombstone = type.Tombstone,
        EventStore = store,
        Namespace = @namespace,
        EventSourceType = string.IsNullOrEmpty(sourceType) ? "Default" : sourceType,
        EventStreamType = string.IsNullOrEmpty(streamType) ? "All" : streamType,
        EventStreamId = string.IsNullOrEmpty(streamId) ? "Default" : streamId,
        Occurred = new DateTimeOffset(2021, 2, 3, 4, 5, 6, TimeSpan.Zero),
        CorrelationId = Guid.Parse("87a96e91-9c76-4eec-abbb-e3555f34e76b"),
        Subject = "stored-subject",
        Tags = ["stored-tag"],
        Hash = "stored-hash",
        Causation = [new() { Occurred = new DateTimeOffset(2021, 2, 3, 4, 5, 5, TimeSpan.Zero), Type = "HTTP", Properties = new Dictionary<string, string> { ["method"] = "POST" } }],
        CausedBy = new() { Subject = "stored-caller", Name = "Stored Caller", UserName = "stored-user", OnBehalfOf = new() { Subject = "original-caller", Name = "Original Caller", UserName = "original-user" } },
        ObservationState = Contracts.Events.EventObservationState.Initial
    };
}
