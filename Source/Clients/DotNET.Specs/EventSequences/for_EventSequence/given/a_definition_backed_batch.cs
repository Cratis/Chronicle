// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Identities;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

/// <summary>
/// An event sequence with two event source definitions, using the real optimistic strategy over a tail that moves on every read.
/// </summary>
public class a_definition_backed_batch : all_dependencies
{
    protected EventSequence _eventSequence;
    protected EventSourceId _sourceId;
    protected IEventSequence _tailSource;
    protected IEventSources _eventSources;
    protected Contracts.Sequences.AppendManyForEventSourcesRequest _request;
    protected Exception _exception;
    ulong _tail = 10;

    protected static EventSourceDefinition Cart => new(
        typeof(EventSources.for_EventSources.ShoppingCartEventSource),
        "ShoppingCart",
        string.Empty,
        ConcurrencyDimensions.EventSourceId,
        [
            new EventStream("Items", string.Empty, ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType | ConcurrencyDimensions.EventStreamId),
            new EventStream("Payment", string.Empty, ConcurrencyDimensions.None)
        ]);

    protected static EventSourceDefinition Order => new(
        typeof(EventSources.for_EventSources.OrderEventSource),
        "Order",
        string.Empty,
        ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventSourceType,
        []);

    void Establish()
    {
        _sourceId = EventSourceId.New();
        _tailSource = Substitute.For<IEventSequence>();
        _tailSource
            .GetTailSequenceNumber(Arg.Any<EventSourceId?>(), Arg.Any<EventSourceType?>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(_ => Task.FromResult<EventSequenceNumber>(_tail++));
        var strategy = new OptimisticConcurrencyStrategy(_tailSource);
        _concurrencyScopeStrategies.GetFor(Arg.Any<IEventSequence>()).Returns(strategy);

        _eventSources = Substitute.For<IEventSources>();
        _eventSources.GetFor(typeof(EventSources.for_EventSources.ShoppingCartEventSource)).Returns(Cart);
        _eventSources.GetFor(typeof(EventSources.for_EventSources.OrderEventSource)).Returns(Order);

        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("event", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [1, 2], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
        });

        _eventSequence = new(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            _connection,
            _eventTypes,
            _constraints,
            _eventSerializer,
            _correlationIdAccessor,
            _concurrencyScopeStrategies,
            _causationManager,
            _unitOfWorkManager,
            _identityProvider,
            JsonSerializerOptions.Default,
            eventSources: _eventSources);
    }

    protected EventForEventSourceId Item(string streamId) =>
        new(_sourceId, "item") { EventSource = typeof(EventSources.for_EventSources.ShoppingCartEventSource), EventStream = "Items", EventStreamId = streamId };

    protected EventForEventSourceId Payment() =>
        new(_sourceId, "payment") { EventSource = typeof(EventSources.for_EventSources.ShoppingCartEventSource), EventStream = "Payment" };

    protected EventForEventSourceId CartWithoutStream(string streamId) =>
        new(_sourceId, "cart") { EventSource = typeof(EventSources.for_EventSources.ShoppingCartEventSource), EventStreamId = streamId };

    protected EventForEventSourceId OrderEvent() =>
        new(_sourceId, "order") { EventSource = typeof(EventSources.for_EventSources.OrderEventSource) };

    protected async Task Append(IEnumerable<EventForEventSourceId> events, IDictionary<EventSourceId, ConcurrencyScope>? scopes = null)
    {
        try
        {
            await _eventSequence.AppendMany(events, concurrencyScopes: scopes);
        }
        catch (Exception ex)
        {
            _exception = ex;
        }
    }
}
