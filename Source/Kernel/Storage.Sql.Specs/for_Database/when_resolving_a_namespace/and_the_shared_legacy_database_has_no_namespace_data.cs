// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_the_shared_legacy_database_has_no_namespace_data(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _legacy;
    string _storeName;
    string _namespaceName;

    async Task Establish()
    {
        Store = new string('s', 90);
        _legacy = LogicalStoreName[..63];
        await SeedStore(Store);
        await CreateLegacyDatabase(_legacy);
    }

    async Task Because()
    {
        await using var ns = await Database.Namespace(Store, Namespace);
        _namespaceName = ns.DbContext.Database.GetDbConnection().Database;
        await using var store = await Database.EventStore(Store);
        _storeName = store.DbContext.Database.GetDbConnection().Database;
    }

    [Fact] void should_keep_the_legacy_store_database() => _storeName.ShouldEqual(_legacy);
    [Fact] void should_separate_the_empty_namespace() => _namespaceName.ShouldNotEqual(_legacy);
}
