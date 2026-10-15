// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_successful_explicit_acquisition : given.a_protected_reader
{
    DecisionRead<Model> _read;
    Exception _aggregateError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        _read = await _reader.GetFor<Model>(_unit, "source");
        _aggregateError = Catch.Exception(() => _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", new ConcurrencyScope(4, EventSourceId: "source"), _store.Name, _store.Namespace, EventSequenceId.Log, "Aggregate")));
    }

    [Fact] void should_return_the_fresh_instance() => _read.Instance.ShouldNotBeNull();
    [Fact] void should_enroll_before_returning_the_instance() => _aggregateError.ShouldBeOfExactType<ReadAndAggregateOnSameSource>();
}
