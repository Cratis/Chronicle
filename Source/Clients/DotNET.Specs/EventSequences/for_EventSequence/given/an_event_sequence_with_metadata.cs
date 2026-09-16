// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_metadata : an_event_sequence
{
    protected readonly EventSourceId _source = "source";
    protected readonly CorrelationId _correlationId = Guid.Parse("2512583c-5abc-4500-9cc8-1ec59a972b4f");
    protected readonly DateTimeOffset _occurred = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
    protected Contracts.Sequences.AppendRequest _request;
    protected Contracts.Sequences.AppendManyForEventSourcesRequest _batchRequest;
    protected AppendedEventWithResult[] _notifications;
    IDisposable _subscription;

    void Establish()
    {
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(new Identity("caller", "Caller", "caller", null));
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendRequest>();
            return CommandResult<Contracts.Sequences.AppendResponse>.Success(_correlationId, new() { CorrelationId = _correlationId, SequenceNumber = 42, ConstraintViolations = [], Errors = [] });
        });
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _batchRequest = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(_correlationId, new() { CorrelationId = _correlationId, SequenceNumbers = [42, 43], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
        });
        _subscription = _eventSequence.AppendOperations.Subscribe(events => _notifications = events.ToArray());
    }

    void Destroy() => _subscription.Dispose();
}
