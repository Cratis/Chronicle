// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Contracts.Commands;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Identities;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection.given;

public class a_trusted_commit : for_EventSequence.given.all_dependencies
{
    protected EventSequence _sequence;
    protected UnitOfWork _unit;
    protected DecisionReadCommitOwner _owner;
    protected EventType _changed;
    protected Contracts.Sequences.AppendManyForEventSourcesRequest _request;
    protected Exception _error;
    protected bool _onCompletedCalled;

    void Establish()
    {
        ((Connections.IKernelCapabilities)_connection).Capabilities.Returns([Contracts.Clients.KernelCapabilities.ConcurrencyScopeConjunctions]);
        _changed = new("changed", EventTypeGeneration.First);
        _eventTypes.HasFor(typeof(string)).Returns(true);
        _eventTypes.GetEventTypeFor(typeof(string)).Returns(_changed);
        _eventSerializer.Serialize(Arg.Any<object>()).Returns(new JsonObject());
        _identityProvider.GetCurrent().Returns(Identity.NotSet);
        _sequences.TailSequenceNumber(Arg.Any<Contracts.Sequences.TailSequenceNumberRequest>(), Arg.Any<CallContext>())
            .Returns(Contracts.Queries.QueryResult<Contracts.Sequences.EventSequenceTailResponse>.Success(Guid.NewGuid(), new() { SequenceNumber = 40 }));
        _sequences.AppendManyForEventSources(Arg.Any<Contracts.Sequences.AppendManyForEventSourcesRequest>(), Arg.Any<CallContext>()).Returns(call =>
        {
            _request = call.Arg<Contracts.Sequences.AppendManyForEventSourcesRequest>();
            return CommandResult<Contracts.Sequences.AppendManyResponse>.Success(Guid.NewGuid(), new() { SequenceNumbers = [101, 102], ConstraintViolations = [], ConcurrencyViolations = [], Errors = [] });
        });
        _sequence = new("store", "tenant", EventSequenceId.Log, _connection, _eventTypes, _constraints, _eventSerializer, _correlationIdAccessor, _concurrencyScopeStrategies, _causationManager, _unitOfWorkManager, _identityProvider, JsonSerializerOptions.Default);
        _concurrencyScopeStrategies.GetFor(_sequence).Returns(new OptimisticConcurrencyStrategy(_sequence));
        var store = Substitute.For<IEventStore>();
        store.Name.Returns((EventStoreName)"store");
        store.Namespace.Returns((EventStoreNamespaceName)"tenant");
        store.EventTypes.Returns(_eventTypes);
        store.GetEventSequence(EventSequenceId.Log).Returns(_sequence);
        _unit = new(CorrelationId.New(), _ => _onCompletedCalled = true, store);
        _owner = _unit.ClaimDecisionReadCommitOwnership();
    }

    protected async Task Execute(Func<Task> action)
    {
        using var frame = _unit.EnforceReadProtection();
        _error = await Catch.Exception(action);
    }

    protected void Read() => _unit.AddDecisionRead(new DecisionRead<object>("source", null, "store", "tenant", 100, [_changed]));

    protected void EnrollReadScope() => _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Explicit, "source", new(100, "source"), "store", "tenant", EventSequenceId.Log, "stream read"));

    protected void ValidateOnly() => _unit.AddEvents(EventSequenceId.Log, [], [new("source", new ConcurrencyScope(100, "source"))]);

    protected Task Commit() => _unit.CommitAsOwner(_owner);
}
