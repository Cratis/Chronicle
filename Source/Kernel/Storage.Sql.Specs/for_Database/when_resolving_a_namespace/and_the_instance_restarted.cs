// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_the_instance_restarted(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string _original;
    string _restarted;

    async Task Establish()
    {
        Store = new string('s', 90);
        await using var scope = await Database.Namespace(Store, Namespace);
        _original = scope.DbContext.Database.GetDbConnection().Database;
    }

    async Task Because()
    {
        await using var scope = await CreateDatabase().Namespace(Store, Namespace);
        _restarted = scope.DbContext.Database.GetDbConnection().Database;
    }

    [Fact] void should_keep_the_permanent_binding() => _restarted.ShouldEqual(_original);
}
