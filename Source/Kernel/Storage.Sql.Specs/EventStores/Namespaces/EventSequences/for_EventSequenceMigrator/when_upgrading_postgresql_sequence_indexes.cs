// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceMigrator;

[PerFactSpecificationLifecycle]
[Collection(PostgreSqlCollection.Name)]
public class when_upgrading_postgresql_sequence_indexes(PostgreSqlFixture fixture) : given.a_postgresql_index_catalog(fixture)
{
    [Fact]
    async Task should_create_a_fresh_table_without_the_redundant_index()
    {
        (await LegacyIndex()).ShouldBeEmpty();
        var indexes = await Inventory();
        indexes.Length.ShouldEqual(7); // Primary key and six retained access paths.
        indexes.Any(index => index.Contains("UNIQUE INDEX", StringComparison.Ordinal) && index.Contains("(\"SequenceNumber\")", StringComparison.Ordinal)).ShouldBeTrue();
        await using var context = Context();
        context.Model.FindEntityType(typeof(EventEntry))!.FindProperty(nameof(EventEntry.SequenceNumber))!.GetColumnType().ShouldEqual("bigint");
    }

    [Fact]
    async Task should_remove_only_the_proven_legacy_index_and_survive_restarts()
    {
        var retained = await Inventory();
        await AddLegacyIndex();
        (await LegacyIndex()).Length.ShouldEqual(1);
        await using (var context = Context())
        {
            context.Events.Add(new EventEntry { SequenceNumber = 7, Content = "{}" });
            await context.SaveChangesAsync();
        }
        await Restart();
        (await LegacyIndex()).ShouldBeEmpty();
        Assert.Equal(retained, await Inventory());
        await Restart();
        Assert.Equal(retained, await Inventory());
        await using var restarted = Context();
        (await restarted.Events.SingleAsync()).SequenceNumber.ShouldEqual(7UL);
    }

    [Fact]
    async Task should_be_idempotent_when_the_index_is_already_absent()
    {
        var retained = await Inventory();
        await Restart();
        await Restart();
        Assert.Equal(retained, await Inventory());
    }

    [Fact]
    async Task should_retain_the_index_when_the_primary_key_is_absent()
    {
        await AddLegacyIndex();
        var constraint = (await Read("SELECT conname AS \"Value\" FROM pg_constraint WHERE conrelid = (SELECT oid FROM pg_class WHERE relname = @table AND relnamespace = current_schema()::regnamespace) AND contype = 'p'")).Single();
        await Execute($"ALTER TABLE {Quote(Table)} DROP CONSTRAINT {Quote(constraint)}");
        var retained = await Inventory();
        await Restart();
        Assert.Equal(retained, await Inventory());
    }

    [Fact]
    async Task should_scope_the_drop_to_the_current_schema_and_table()
    {
        await AddLegacyIndex();
        await Execute($"CREATE SCHEMA other; CREATE TABLE other.{Quote(Table)} (\"SequenceNumber\" numeric(20,0) PRIMARY KEY); CREATE INDEX {Quote($"IX_{Table}_SequenceNumber")} ON other.{Quote(Table)} (\"SequenceNumber\")");
        await Execute("CREATE TABLE unrelated (\"SequenceNumber\" numeric(20,0) PRIMARY KEY); CREATE INDEX unrelated_sequence ON unrelated (\"SequenceNumber\")");
        var others = await Read("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'other' OR tablename = 'unrelated' ORDER BY schemaname, indexname");
        await Restart();
        (await LegacyIndex()).ShouldBeEmpty();
        Assert.Equal(others, await Read("SELECT indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = 'other' OR tablename = 'unrelated' ORDER BY schemaname, indexname"));
    }

    [Theory]
    [InlineData("(\"SequenceNumber\" DESC)")]
    [InlineData("(\"SequenceNumber\") WHERE \"SequenceNumber\" > 0")]
    [InlineData("(\"SequenceNumber\") INCLUDE (\"Type\")")]
    [InlineData("((\"SequenceNumber\" + 1))")]
    [InlineData("(\"SequenceNumber\", \"Type\")")]
    [InlineData("(\"Occurred\")")]
    [InlineData("USING brin (\"SequenceNumber\")")]
    [InlineData("(\"SequenceNumber\") WITH (fillfactor = 70)")]
    async Task should_retain_a_custom_definition_with_the_legacy_name(string definition)
    {
        await AddLegacyIndex(definition);
        var retained = await Inventory();
        await Restart();
        Assert.Equal(retained, await Inventory());
    }

    [Fact]
    async Task should_retain_an_equivalent_index_not_owned_by_the_legacy_migration()
    {
        await Execute($"CREATE INDEX custom_sequence ON {Quote(Table)} (\"SequenceNumber\")");
        var retained = await Inventory();
        await Restart();
        Assert.Equal(retained, await Inventory());
    }

    [Fact]
    async Task should_retain_a_unique_index_with_the_legacy_name()
    {
        await Execute($"CREATE UNIQUE INDEX {Quote($"IX_{Table}_SequenceNumber")} ON {Quote(Table)} (\"SequenceNumber\")");
        var retained = await Inventory();
        await Restart();
        Assert.Equal(retained, await Inventory());
    }
}
