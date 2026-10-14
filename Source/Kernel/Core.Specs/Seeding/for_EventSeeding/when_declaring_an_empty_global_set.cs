// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_declaring_an_empty_global_set : given.an_owned_global_seed_set
{
    Task Because() => _grain.SeedGlobalWithResult([], true, "program");

    [Fact] void should_keep_only_other_declarations_and_additions() => StoredSources.ShouldContainOnly([AnEntry(2).EventSourceId, AnEntry(3).EventSourceId]);
    [Fact] void should_not_touch_namespace_grains() => _namespaceGrain.DidNotReceive().SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_persist_the_removal() => _state.Received(1).WriteStateAsync();
}
