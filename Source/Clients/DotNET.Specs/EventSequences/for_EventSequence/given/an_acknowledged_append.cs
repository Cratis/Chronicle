// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Commands;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_acknowledged_append : an_event_sequence_with_metadata
{
    protected Contracts.Sequences.AppendResponse _response;
    protected Contracts.Sequences.AppendManyResponse _batchResponse;
    protected Contracts.Sequences.AppendManyRequest _legacyRequest;
    protected Exception _error;

    void Establish()
    {
        var type = new Contracts.Sequences.EventType { Id = "event", Generation = 1 };
        _response = new() { SequenceNumber = 42, Receipt = append_receipts.Create(42, _source, type, _eventStoreName, _namespace) };
        _batchResponse = new()
        {
            SequenceNumbers = [42, 47],
            Receipts = [append_receipts.Create(42, _source, type, _eventStoreName, _namespace), append_receipts.Create(47, _source, type, _eventStoreName, _namespace)]
        };
        _notifications = [];
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendRequest>();
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(_correlationId, _response);
        });
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _batchRequest = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(_correlationId, _batchResponse);
        });
        _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _legacyRequest = call.Arg<Contracts.Sequences.AppendManyRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(_correlationId, _batchResponse);
        });
    }
}
