// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_adding_non_authoritative_globals : given.an_owned_global_seed_set
{
    Task Because() => _grain.SeedGlobalWithResult([AnEntry(5)], false, "program");

    [Fact] void should_keep_all_previous_entries() => StoredSources.ShouldContainOnly(Enumerable.Range(1, 5).Select(_ => AnEntry(_).EventSourceId));
    [Fact] void should_record_an_explicit_addition() => _stored.ByEventSource[AnEntry(5).EventSourceId].Single().Origin.ShouldEqual(SeedOrigin.Added);
    [Fact] void should_record_the_sender() => _stored.ByEventSource[AnEntry(5).EventSourceId].Single().Owner.ShouldEqual("program");
}
