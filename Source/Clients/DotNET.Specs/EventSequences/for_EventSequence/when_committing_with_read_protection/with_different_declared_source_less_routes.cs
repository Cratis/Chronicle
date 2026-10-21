// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSources;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_committing_with_read_protection;

public class with_different_declared_source_less_routes : given.a_trusted_commit
{
    Task Because() => Execute(async () =>
    {
        Read();
        _unit.AddEvents(
            EventSequenceId.Log,
            [
                new("source", "first") { EventStreamType = "first", DeclaredConcurrency = ConcurrencyDimensions.EventStreamType },
                new("source", "second") { EventStreamType = "second", DeclaredConcurrency = ConcurrencyDimensions.EventStreamType }
            ],
            []);
        await Commit();
    });

    [Fact] void should_commit_successfully() => _error.ShouldBeNull();
    [Fact] void should_keep_three_conditions() => _request.ConcurrencyScopes.Count().ShouldEqual(3);
    [Fact] void should_keep_each_routing_boundary() => _unit.GetAppendConditions().Where(_ => _.EventSourceId is null).Select(_ => _.After.Value).ShouldEqual([40UL, 40UL]);
    [Fact] void should_keep_each_routing_predicate() => _unit.GetAppendConditions().Where(_ => _.EventSourceId is null).Select(_ => _.EventStreamType.Value).ShouldEqual(["first", "second"]);
}
