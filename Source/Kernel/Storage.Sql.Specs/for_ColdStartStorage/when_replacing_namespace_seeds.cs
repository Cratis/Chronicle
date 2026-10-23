// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_replacing_namespace_seeds : given.a_cold_start_database
{
    int _count;
    EventSeeds _saved;

    async Task Because()
    {
        var storage = new EventStores.Namespaces.Seeding.EventSeedingStorage(Store, Namespace, _database, new JsonSerializerOptions());
        var seeds = new EventSeeds(new Dictionary<Concepts.Events.EventTypeId, IEnumerable<SeededEventEntry>> { ["type"] = [] }, new Dictionary<Concepts.Events.EventSourceId, IEnumerable<SeededEventEntry>>());
        var competing = new EventSeeds(new Dictionary<Concepts.Events.EventTypeId, IEnumerable<SeededEventEntry>> { ["other"] = [] }, new Dictionary<Concepts.Events.EventSourceId, IEnumerable<SeededEventEntry>>());
        await Race(() => storage.Save(seeds), () => storage.Save(competing));
        _count = await _namespace.EventSeeds.CountAsync();
        _saved = await storage.Get();
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_seed_data() => _saved.ByEventType.ContainsKey("type").ShouldBeTrue();
}
