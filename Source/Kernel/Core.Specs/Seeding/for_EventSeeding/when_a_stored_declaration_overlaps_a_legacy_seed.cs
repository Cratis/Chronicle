// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Seeding;

namespace Cratis.Chronicle.Seeding.for_EventSeeding;

public class when_a_stored_declaration_overlaps_a_legacy_seed : given.overlapping_global_seed_groups
{
    async Task Establish()
    {
        await _grain.SeedGlobalWithResult([AnEntry(1)], true, "program");
        var declared = _stored.ByEventSource.Values.Single().Single();
        var legacy = declared with { Origin = SeedOrigin.Unknown, Owner = string.Empty };
        _stored.ByEventSource[declared.EventSourceId] = [declared, legacy];
        _stored.ByEventType[declared.EventTypeId] = [declared, legacy];
    }

    Task Because() => ApplyToFutureNamespace();

    [Fact] void should_seed_future_namespaces_once() => _futureCount.ShouldEqual(1);
    [Fact] void should_not_modify_the_stored_provenance() => _stored.ByEventSource.Values.Single().Count().ShouldEqual(2);
}
