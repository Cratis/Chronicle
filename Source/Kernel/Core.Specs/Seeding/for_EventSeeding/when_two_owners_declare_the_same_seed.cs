// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_two_owners_declare_the_same_seed : given.overlapping_global_seed_groups
{
    Task Establish() => _grain.SeedGlobalWithResult([AnEntry(1)], true, "first");

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "second");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_seed_existing_namespaces_once() => AppendedCount(_existingEvents).ShouldEqual(1);
    [Fact] void should_seed_future_namespaces_once() => _futureCount.ShouldEqual(1);
    [Fact] void should_preserve_both_owners() => _stored.ByEventSource.Values.SelectMany(_ => _).Select(_ => _.Owner).ShouldContainOnly(["first", "second"]);
}
