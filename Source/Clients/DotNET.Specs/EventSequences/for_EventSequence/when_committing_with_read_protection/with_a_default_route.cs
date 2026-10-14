// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_a_default_route : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(EventSequenceId.Log, [new("source", "event")], []);
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_send_the_read_boundary() => _request.ConcurrencyScopes.Single().Scope.SequenceNumber.ShouldEqual(100UL);
    [Fact] void should_capture_the_actual_conditions() => _unit.GetAppendConditions().Single().After.Value.ShouldEqual(100UL);
}
