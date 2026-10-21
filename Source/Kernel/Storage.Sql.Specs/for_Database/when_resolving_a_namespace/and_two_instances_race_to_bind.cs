// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_a_namespace;

[Collection(PostgreSqlCollection.Name)]
public class and_two_instances_race_to_bind(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    string[] _names;

    void Establish() => Store = new string('s', 90);

    async Task Because()
    {
        var instances = new[] { Database, CreateDatabase() };
        _names = await Task.WhenAll(instances.Select(async database =>
        {
            await using var scope = await database.Namespace(Store, Namespace);
            return scope.DbContext.Database.GetDbConnection().Database;
        }));
    }

    [Fact] void should_agree_on_the_binding() => _names.Distinct(StringComparer.Ordinal).Count().ShouldEqual(1);
}
