// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_a_declaration_overlaps_legacy_seeds : given.overlapping_global_seed_groups
{
    async Task Establish()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], false, string.Empty);
        var legacy = _stored.ByEventSource.Values.Single().Single() with { Origin = SeedOrigin.Unknown };
        _stored.ByEventSource[legacy.EventSourceId] = [legacy];
        _stored.ByEventType[legacy.EventTypeId] = [legacy];
    }

    async Task Because()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "program");
        await ApplyToFutureNamespace();
    }

    [Fact] void should_not_repeat_the_legacy_event_in_existing_namespaces() => AppendedCount(_existingEvents).ShouldEqual(1);
    [Fact] void should_seed_future_namespaces_once() => _futureCount.ShouldEqual(1);
    [Fact] void should_replace_legacy_provenance() => _stored.ByEventSource.Values.Single().Single().Origin.ShouldEqual(SeedOrigin.Declared);
}
