// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Transactions.for_UnitOfWork.given;

public class a_unit_with_implicit_scopes : EventSequences.for_EventSequence.given.a_definition_backed_batch
{
    protected UnitOfWork _unit;
    protected IUnitOfWork _enrollment;
    protected IEventStore _store;
    protected virtual bool Ordered => false;

    void Establish()
    {
        _store = Substitute.For<IEventStore>();
        _store.Name.Returns((EventStoreName)"store");
        _store.Namespace.Returns((EventStoreNamespaceName)"namespace");
        _store.GetEventSequence(Arg.Any<EventSequenceId>()).Returns(_eventSequence);
        _unit = new(CorrelationId.New(), _ => { }, _store);
        _enrollment = _unit;
    }

    protected void Enroll(ConcurrencyScope? scope = null, ConcurrencyScopeOrigin origin = ConcurrencyScopeOrigin.Explicit, EventSourceId? source = null)
    {
        source ??= _sourceId;
        if (Ordered)
        {
            _enrollment.AddEvents(EventSequenceId.Log, [new EventForEventSourceId(source, "event")], scope is null ? [] : [new(source, scope)], origin);
        }
        else
        {
            _enrollment.AddEventWithOptions(EventSequenceId.Log, source, "event", Causation.Unknown(), new(ConcurrencyScope: scope, Origin: origin));
        }
    }

    protected void Guard(EventSequenceNumber? boundary = null, EventSourceId? source = null, EventSequenceId? sequence = null) =>
        _enrollment.AddImplicitScope(sequence ?? EventSequenceId.Log, source ?? _sourceId, boundary ?? (EventSequenceNumber)5);

    protected void Read(EventSequenceNumber boundary) => _unit.AddDecisionRead(new DecisionRead<object>(
        _sourceId.Value, null, _store.Name, _store.Namespace, boundary, [new EventType("read", EventTypeGeneration.First)]));

    protected Contracts.Sequences.ConcurrencyScope ScopeFor(EventSourceId source) => _request.ConcurrencyScopes.Single(_ => _.EventSourceId == source.Value).Scope;

    protected async Task Commit(bool owner = false) => _exception = await Catch.Exception(() => owner
        ? _unit.CommitAsOwner(_unit.ClaimDecisionReadCommitOwnership())
        : _unit.Commit());
}
