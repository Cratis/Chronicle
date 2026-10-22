// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Storage.InMemory.Generations.for_GenerationFloorStorage;

public class when_racing_advancements : given.a_floor_storage
{
    GenerationFloorAdvanceResult[] _results;
    IReadOnlyList<GenerationFloor> _history;
    async Task Establish() => await _storage.Advance(Request());
    async Task Because()
    {
        var target = _floor with { MinimumWriterProtocol = new(2) };
        var stores = new[] { CreateStorage(), CreateStorage() };
        _results = await Task.WhenAll(stores.Select(storage => Task.Run(() => storage.Advance(Request(target, 1)))));
        _history = await _storage.GetHistory();
    }
    [Fact] void should_commit_exactly_once() => _results.Count(_ => _.IsT0).ShouldEqual(1);
    [Fact] void should_commit_only_one_new_audit_entry() => _history.Count.ShouldEqual(2);
    [Fact] void should_commit_the_next_revision() => _history[^1].Revision.ShouldEqual(2UL);
}
