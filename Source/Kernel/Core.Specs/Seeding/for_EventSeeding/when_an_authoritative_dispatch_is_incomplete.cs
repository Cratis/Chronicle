// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_an_authoritative_dispatch_is_incomplete : given.an_owned_global_seed_set
{
    SeedingResult _result;

    void Establish() => _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
    async Task Because() => _result = await _grain.SeedGlobalWithResult([AnEntry(5)], true, "program");

    [Fact] void should_report_incomplete_seeding() => _result.AllEntriesSeeded.ShouldBeFalse();
    [Fact] void should_keep_the_previous_set_intact() => StoredSources.ShouldContainOnly(Enumerable.Range(1, 4).Select(_ => AnEntry(_).EventSourceId));
    [Fact] void should_not_persist_a_replacement() => _state.DidNotReceive().WriteStateAsync();
}
