// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_replacing_an_authoritative_global_set : given.an_owned_global_seed_set
{
    Task Because() => _grain.SeedGlobalWithResult([AnEntry(5)], true, "program");

    [Fact] void should_replace_the_owners_previous_declaration() => StoredSources.ShouldNotContain(AnEntry(1).EventSourceId);
    [Fact] void should_keep_the_other_owners_declaration() => StoredSources.ShouldContain(AnEntry(2).EventSourceId);
    [Fact] void should_keep_explicit_additions() => StoredSources.ShouldContain(AnEntry(3).EventSourceId);
    [Fact] void should_clear_legacy_entries() => StoredSources.ShouldNotContain(AnEntry(4).EventSourceId);
    [Fact] void should_record_the_new_declaration() => _stored.ByEventSource[AnEntry(5).EventSourceId].Single().Origin.ShouldEqual(SeedOrigin.Declared);
    [Fact] void should_record_the_owner() => _stored.ByEventSource[AnEntry(5).EventSourceId].Single().Owner.ShouldEqual("program");
    [Fact] void should_update_both_groupings() => _stored.ByEventType.Values.SelectMany(_ => _).Select(_ => _.EventSourceId).ShouldContainOnly(StoredSources);
    [Fact] void should_dispatch_only_new_entries() => _namespaceGrain.Received(1).SeedWithResult(Arg.Is<IEnumerable<SeedingEntry>>(_ => _.Single().EventSourceId == AnEntry(5).EventSourceId));
    [Fact] void should_persist_the_replacement() => _state.Received(1).WriteStateAsync();
}
