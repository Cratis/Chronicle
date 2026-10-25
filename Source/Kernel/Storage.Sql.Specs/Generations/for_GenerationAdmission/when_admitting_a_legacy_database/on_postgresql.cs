// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationAdmission.when_admitting_a_legacy_database;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_provider(fixture)
{
    string _eventStoreConnection;
    string[] _before;
    string[] _after;

    async Task Establish()
    {
        await using var scope = await Provider.EventStore(Name);
        await scope.DbContext.Database.ExecuteSqlRawAsync("DROP TABLE \"GenerationFloor\"");
        _eventStoreConnection = scope.DbContext.Database.GetConnectionString()!;
        var history = scope.DbContext.GetService<IHistoryRepository>();
        await scope.DbContext.Database.ExecuteSqlRawAsync(history.GetDeleteScript("ES-Generations-v19_107_0"));
        _before = (await scope.DbContext.Database.GetAppliedMigrationsAsync()).ToArray();
    }

    async Task Because()
    {
        await Admit();
        await using var context = new EventStores.EventStoreDbContext(SpecDbContextOptions.Kernel<EventStores.EventStoreDbContext>(_eventStoreConnection));
        _after = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
    }

    [Fact] void should_not_apply_the_generation_migration() => _after.ShouldEqual(_before);
    [Fact] void should_not_have_a_generation_migration_receipt() => _after.Contains("ES-Generations-v19_107_0").ShouldBeFalse();
    [Fact] async Task should_not_establish_a_floor() => (await Provider.ReadGenerationFloors()).ShouldBeEmpty();
}
