// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_different_declared_routes_without_reads : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        _unit.AddEvents(
            EventSequenceId.Log,
            [
                new("source", "first") { EventStreamType = "first", DeclaredConcurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType },
                new("source", "second") { EventStreamType = "second", DeclaredConcurrency = ConcurrencyDimensions.EventSourceId | ConcurrencyDimensions.EventStreamType }
            ],
            []);
        await Commit();
    });

    [Fact] void should_accept_each_predicate() => _error.ShouldBeNull();
    [Fact] void should_send_both_guards() => _request.ConcurrencyScopes.Count().ShouldEqual(2);
}
