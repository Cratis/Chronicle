// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_required_read_model_indexes_are_not_created : given.a_schema_provisioner
{
    Exception? _markerFailure;
    Exception? _shadowFailure;
    SchemaReport _recovered;
    SchemaReport _missingDecimalShadow;
    SchemaReport _restored;
    bool _shadowStillMissing;

    async Task Because()
    {
        await _provisioner.Migrate(Scope);
        await given.RegisteredDynamicSchema.Register(_schemaDatabase, "orders", "production");
        await using (var registry = await _schemaDatabase.EventStore("orders"))
        {
            var definition = await registry.DbContext.ReadModels.SingleAsync();
            definition.IndexesJson = "[\"amount\"]";
            await registry.DbContext.SaveChangesAsync();
        }
        _markerFailure = await FailMigration(false);
        _shadowFailure = await FailMigration(true);
        await _provisioner.Migrate(Scope);
        _recovered = await _provisioner.Verify(Scope);
        await using var model = await _schemaDatabase.ReadModelTable("orders", "production", given.RegisteredDynamicSchema.Primary, given.RegisteredDynamicSchema.Columns);
        await DropIndexes(model.DbContext, "chronicle_shx_%");
        _missingDecimalShadow = await _provisioner.Verify(Scope);
        _shadowStillMissing = !await model.DbContext.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({model.DbContext.TableName}) WHERE name LIKE 'chronicle_shx_%'").AnyAsync();
        await _provisioner.Migrate(Scope);
        _restored = await _provisioner.Verify(Scope);
    }

    async Task<Exception?> FailMigration(bool shadow)
    {
        var real = _services.GetRequiredService<IReadModelMigrator>();
        var incomplete = Substitute.For<IReadModelMigrator>();
        incomplete.EnsureReplayPromotions(Arg.Any<ReadModelDbContext>()).Returns(call => real.EnsureReplayPromotions(call.Arg<ReadModelDbContext>()));
        incomplete.When(migrator => migrator.ClearMigrationCache(Arg.Any<string>())).Do(call => real.ClearMigrationCache(call.Arg<string>()));
        incomplete.EnsureTableMigrated(Arg.Any<string>(), Arg.Any<ReadModelDbContext>()).Returns(async call =>
        {
            var context = call.Arg<ReadModelDbContext>();
            await real.EnsureTableMigrated(call.Arg<string>(), context);
            var prefix = shadow ? "chronicle_shx_%" : "chronicle_imx_%";
            await DropIndexes(context, prefix);
        });
        var database = new Database(
            _services,
            Options.Create(new ChronicleOptions { Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = _connectionString, SchemaMode = SchemaMode.Verify } }),
            _services.GetRequiredService<IEventSequenceMigrator>(),
            _services.GetRequiredService<IUniqueConstraintMigrator>(),
            incomplete);
        return await Catch.Exception(() => new SchemaProvisioner(database, NullLogger<SchemaProvisioner>.Instance).Migrate(Scope));
    }

    static async Task DropIndexes(ReadModelDbContext context, string prefix)
    {
        var indexes = await context.Database.SqlQuery<string>($"SELECT name AS Value FROM pragma_index_list({context.TableName}) WHERE name LIKE {prefix}").ToArrayAsync();
        var migration = new MigrationBuilder(context.Database.ProviderName);
        foreach (var index in indexes) migration.DropIndex(index, context.TableName);
        await new EventStores.Namespaces.TableMigrator<ReadModelDbContext>(NullLogger<EventStores.Namespaces.TableMigrator<ReadModelDbContext>>.Instance).ExecuteMigrationOperations(context, migration);
    }

    [Fact] void should_fail_explicit_migration_when_the_marker_index_is_deferred() => _markerFailure.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_identify_the_required_marker_index() => ((SchemaNotCurrent)_markerFailure!).Pending.Any(item => item.Contains("initialized-marker", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_fail_explicit_migration_when_the_shadow_index_is_not_created() => _shadowFailure.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_identify_the_required_shadow_index() => ((SchemaNotCurrent)_shadowFailure!).Pending.Any(item => item.Contains("shadow", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_allow_a_later_successful_migration() => _recovered.Scopes.SelectMany(scope => scope.Pending).ShouldBeEmpty();
    [Fact] void should_report_a_missing_decimal_shadow_as_a_missing_declared_index() => _missingDecimalShadow.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains("declared index", StringComparison.Ordinal) && item.Contains("amount", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_repair_the_decimal_shadow_during_verification() => _shadowStillMissing.ShouldBeTrue();
    [Fact] void should_verify_after_repairing_the_decimal_shadow() => _restored.Scopes.SelectMany(scope => scope.Pending).ShouldBeEmpty();
}
