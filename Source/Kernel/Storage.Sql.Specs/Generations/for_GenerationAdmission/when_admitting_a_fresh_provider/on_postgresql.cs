// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Generations.for_GenerationAdmission.when_admitting_a_fresh_provider;

[Collection(PostgreSqlCollection.Name)]
public class on_postgresql(PostgreSqlFixture fixture) : given.a_postgresql_provider(fixture)
{
    string[] _migrations;

    async Task Because()
    {
        await Admit();
        await using var context = new Cluster.ClusterDbContext(new DbContextOptionsBuilder<Cluster.ClusterDbContext>().UseNpgsql(OptionsForProvider.Value.Storage.ConnectionDetails).AddConceptAsSupport().Options);
        _migrations = (await context.Database.GetAppliedMigrationsAsync()).ToArray();
    }

    [Fact] void should_not_apply_any_migration() => _migrations.ShouldBeEmpty();
    [Fact] async Task should_not_establish_a_floor() => (await Provider.ReadGenerationFloors()).ShouldBeEmpty();
}
