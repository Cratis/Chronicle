// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_creating_namespaces : given.a_cold_start_database
{
    int _count;
    DateTimeOffset _created;
    readonly DateTimeOffset _timestamp = DateTimeOffset.Parse("2026-01-01T12:00:00Z");

    async Task Because()
    {
        var storage = new NamespaceStorage(Store, _database);
        await Race(() => storage.Create(Namespace, _timestamp));
        _count = await _store.Namespaces.CountAsync();
        _created = (await _store.Namespaces.SingleAsync()).Created;
    }

    [Fact] void should_keep_one_row() => _count.ShouldEqual(1);
    [Fact] void should_keep_the_creation_time() => _created.ShouldEqual(_timestamp);
}
