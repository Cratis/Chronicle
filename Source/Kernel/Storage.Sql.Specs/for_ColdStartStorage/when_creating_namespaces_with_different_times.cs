// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ColdStartStorage;

public class when_creating_namespaces_with_different_times : given.a_cold_start_database
{
    DateTimeOffset _created;
    readonly DateTimeOffset _first = DateTimeOffset.Parse("2026-01-01T12:00:00Z");

    async Task Because()
    {
        var storage = new NamespaceStorage(Store, _database);
        await Race(() => storage.Create(Namespace, _first.AddMinutes(1)), () => storage.Create(Namespace, _first));
        await storage.Create(Namespace, _first.AddMinutes(2));
        _created = (await _store.Namespaces.SingleAsync()).Created;
    }

    [Fact] void should_preserve_the_first_creation_time() => _created.ShouldEqual(_first);
}
