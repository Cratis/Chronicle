// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_redeclaring_an_already_seeded_global_entry : given.an_owned_global_seed_set
{
    Task Because() => _grain.SeedGlobalWithResult([AnEntry(1)], true, "program");

    [Fact] void should_retain_the_declared_entry_once() => _stored.ByEventSource[AnEntry(1).EventSourceId].Count().ShouldEqual(1);
    [Fact] void should_not_dispatch_the_entry_again() => _namespaceGrain.DidNotReceive().SeedWithResult(Arg.Any<IEnumerable<SeedingEntry>>());
    [Fact] void should_still_remove_legacy_entries() => StoredSources.ShouldNotContain(AnEntry(4).EventSourceId);
}
