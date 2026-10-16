// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Testing.Events;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.Testing.ReadModels.for_DecisionReadsForTesting;

public class when_an_enforcing_unit_acquires_a_model : Specification
{
    EventStoreForTesting _store;
    EventSourceId _source;
    DecisionRead<SimpleModule> _read;
    Exception _aggregateError;

    async Task Establish()
    {
        _store = new EventStoreForTesting();
        _source = EventSourceId.New();
        await _store.EventLog.Append(_source, new ModuleCreated("Test"));
    }

    async Task Because()
    {
        var unit = _store.UnitOfWorkManager.Begin(CorrelationId.New());
        using var frame = unit.EnforceReadProtection();
        _read = await _store.GetDecisionReads().GetFor<SimpleModule>(unit, _source);
        _aggregateError = Catch.Exception(() => unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, _source, new ConcurrencyScope(EventSequenceNumber.First, EventSourceId: _source), _store.Name, _store.Namespace, EventSequenceId.Log, "Aggregate")));
    }

    [Fact] void should_fold_the_model() => _read.Instance!.Name.ShouldEqual("Test");
    [Fact] void should_enroll_before_exposing_the_model() => _aggregateError.ShouldBeOfExactType<ReadAndAggregateOnSameSource>();
}
