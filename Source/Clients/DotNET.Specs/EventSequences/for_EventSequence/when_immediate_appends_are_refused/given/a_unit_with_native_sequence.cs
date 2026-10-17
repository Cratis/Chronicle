// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.EventSources;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused.given;

public class a_unit_with_native_sequence : for_EventSequence.given.all_dependencies
{
    protected EventSequence _sequence;
    protected IEventStore _store;
    protected UnitOfWorkManager _manager;
    protected UnitOfWork _unit;
    protected IEventSources _eventSources;
    protected Contracts.Sequences.AppendManyForEventSourcesRequest _request;
    protected Exception _error;

    void Establish()
    {
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(new EventType("changed", EventTypeGeneration.First));
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _concurrencyScopeStrategy.GetScope(Arg.Any<EventSourceId>(), Arg.Any<EventStreamType?>(), Arg.Any<EventStreamId?>(), Arg.Any<EventSourceType?>(), Arg.Any<IEnumerable<EventType>?>())
            .Returns(ConcurrencyScope.None);
        _eventSources = Substitute.For<IEventSources>();
        _store = Substitute.For<IEventStore>();
        _store.Name.Returns((EventStoreName)"store");
        _store.Namespace.Returns((EventStoreNamespaceName)"tenant");
        _manager = new(_store);
        _sequence = CreateSequence("store", EventSequenceId.Log);
        _store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _unit = (UnitOfWork)_manager.Begin(CorrelationId.New());
        _sequences.Append(Arg.Any<Contracts.Sequences.AppendRequest>(), Arg.Any<CallContext>())
            .Returns(CommandResult<Contracts.Sequences.AppendResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 101 }));
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>())
            .Returns(call =>
            {
                _request = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
                return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [101], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
            });
        _sequences.AppendManyForEventSourcesWithNamedTags(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesWithNamedTagsRequest>(), Arg.Any<CallContext>())
            .Returns(CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [101], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] }));
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>())
            .Returns(Contracts.Queries.QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 40 }));
    }

    protected ImmediateAppendRefusal Refuse()
    {
        _manager.SetCurrent(_unit);
        return _unit.RefuseImmediateAppends();
    }

    protected EventSequence CreateSequence(EventStoreName store, EventSequenceId sequence) =>
        new(store, "tenant", sequence, _connection, _eventTypes, _constraints, _eventSerializer, _correlationIdAccessor, _concurrencyScopeStrategies, _causationManager, _manager, _identityProvider, JsonSerializerOptions.Default, eventSources: _eventSources);

    protected EventLog CreateLog() =>
        new("store", "tenant", _connection, _eventTypes, _constraints, _eventSerializer, _correlationIdAccessor, _concurrencyScopeStrategies, _causationManager, _manager, _identityProvider, JsonSerializerOptions.Default, eventSources: _eventSources);

    protected void ClearCalls()
    {
        _sequences.ClearReceivedCalls();
        _eventSerializer.ClearReceivedCalls();
        _eventTypes.ClearReceivedCalls();
        _eventSources.ClearReceivedCalls();
        _concurrencyScopeStrategies.ClearReceivedCalls();
        _concurrencyScopeStrategy.ClearReceivedCalls();
        _identityProvider.ClearReceivedCalls();
        _causationManager.ClearReceivedCalls();
    }

    protected static IEnumerable<T> Poison<T>() =>
        Enumerable.Repeat(default(T)!, 1).Select<T, T>(_ => throw new Exception("Append admission must run before enumeration"));
}
