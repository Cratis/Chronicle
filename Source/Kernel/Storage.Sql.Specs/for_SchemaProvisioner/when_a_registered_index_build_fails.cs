// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.EventStores.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_a_registered_index_build_fails : given.a_schema_provisioner
{
    Exception? _exception;
    string _indexName;

    async Task Establish()
    {
        await _provisioner.Migrate(Scope);
        await using var registry = await _schemaDatabase.EventStore("orders");
        registry.DbContext.ReadModels.Add(given.RegisteredDynamicSchema.Definition);
        await registry.DbContext.SaveChangesAsync();
        var definition = given.RegisteredDynamicSchema.Definition.ToKernel();
        var plan = ReadModelIndexPlan.Resolve(definition.Indexes.Single(), definition.GetSchemaForLatestGeneration(), given.RegisteredDynamicSchema.Columns, Cratis.Arc.EntityFrameworkCore.DatabaseType.Sqlite)!;
        _indexName = plan.Name(Cratis.Arc.EntityFrameworkCore.DatabaseType.Sqlite, given.RegisteredDynamicSchema.Primary);

        // SQLite shares table and index names. A table collision makes the real CREATE INDEX fail.
        await using var collision = await _schemaDatabase.ReadModelTable("orders", "production", _indexName, []);
        await collision.DbContext.EnsureTableExists();
    }

    async Task Because() => _exception = await Catch.Exception(() => _provisioner.Migrate(Scope));

    [Fact] void should_propagate_the_index_creation_failure() => _exception.ShouldNotBeNull();
    [Fact] void should_identify_the_failed_index() => _exception!.ToString().Contains(_indexName, StringComparison.Ordinal).ShouldBeTrue();
}
