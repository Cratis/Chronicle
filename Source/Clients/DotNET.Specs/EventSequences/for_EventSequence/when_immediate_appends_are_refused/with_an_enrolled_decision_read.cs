// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_immediate_appends_are_refused;

public class with_an_enrolled_decision_read : given.a_unit_with_native_sequence
{
    async Task Because()
    {
        using var owner = Refuse();
        _unit.AddDecisionRead(new DecisionRead<object>("dependency", null, "store", "tenant", 7, [new EventType("changed", EventTypeGeneration.First)]));
        await _sequence.Transactional.Append("target", "changed");
        ClearCalls();
        await owner.Commit();
    }

    [Fact] void should_preserve_the_read_dependency() => _request.ConcurrencyScopes.Single(scope => scope.EventSourceId == "dependency").Scope.SequenceNumber.ShouldEqual(7UL);
    [Fact] void should_commit_only_once() => _sequences.ReceivedCalls().Count().ShouldEqual(1);
    [Fact] void should_complete_successfully() => _unit.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_turn_on_read_protection() => _unit.IsEnforcingReadProtection.ShouldBeFalse();
}
