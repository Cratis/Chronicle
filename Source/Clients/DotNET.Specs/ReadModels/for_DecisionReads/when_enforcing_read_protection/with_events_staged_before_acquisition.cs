// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_enforcing_read_protection;

public class with_events_staged_before_acquisition : given.a_protected_reader
{
    DecisionRead<Model>? _read;
    Exception _error;

    async Task Because()
    {
        using var frame = _unit.EnforceReadProtection();
        _unit.AddEvent(EventSequenceId.Log, "source", new Created(), Causation.Unknown());
        _error = await Catch.Exception(async () => _read = await _reader.GetFor<Model>(_unit, "source"));
    }

    [Fact] void should_refuse_the_stale_decision() => _error.ShouldBeOfExactType<ReadOfSourceWithStagedEvents>();
    [Fact] void should_not_expose_the_read() => _read.ShouldBeNull();
}
