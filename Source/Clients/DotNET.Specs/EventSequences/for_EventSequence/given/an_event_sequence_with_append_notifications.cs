// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_append_notifications : an_event_sequence
{
    protected object _event;
    protected EventSourceId _eventSourceId;
    protected EventType _eventType;
    protected bool _responseReturned;
    protected IDisposable _subscription;
    protected Exception _error;
    protected AppendResult _result;
    protected AppendManyResult _manyResult;

    void Establish()
    {
        _event = new object();
        _eventSourceId = "source";
        _eventType = new("event", EventTypeGeneration.First);
        _eventTypes.HasFor(typeof(object)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(object)).Returns(_eventType);
        _eventSerializer.Serialize(_event).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), CallContext.Default).Returns(_ =>
        {
            _responseReturned = true;
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 42 });
        });
        _sequences.AppendMany(Arg.Any<Contracts.Sequences.AppendManyRequest>(), CallContext.Default).Returns(_ =>
        {
            _responseReturned = true;
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [42] });
        });
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), CallContext.Default).Returns(_ =>
        {
            _responseReturned = true;
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [42] });
        });
    }

    void Destroy() => _subscription?.Dispose();
}
