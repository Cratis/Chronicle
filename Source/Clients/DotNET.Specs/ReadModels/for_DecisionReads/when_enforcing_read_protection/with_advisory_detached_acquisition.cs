// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.EventSequences.Concurrency;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_advisory_detached_acquisition : given.a_protected_reader
{
    DecisionRead<Model> _read;
    Exception _enrollmentError;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        using (var allowance = _unit.AllowReads(ReadAllowance.Advisory, "Reference context"))
        {
            _read = await _reader.GetDetached<Model>("source");
        }
        _enrollmentError = Catch.Exception(() => _unit.EnrolContributedRead(new(ConcurrencyScopeOrigin.Aggregate, "source", new ConcurrencyScope(4, EventSourceId: "source"), _store.Name, _store.Namespace, EventSequenceId.Log, "Aggregate")));
    }

    [Fact] void should_return_the_context() => _read.Instance.ShouldNotBeNull();
    [Fact] void should_not_enroll_the_detached_read() => _enrollmentError.ShouldBeNull();
}
