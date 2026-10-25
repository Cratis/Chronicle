// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events.Constraints;
using Cratis.Chronicle.Storage.Sql.EventStores.Constraints;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

[Collection(PostgreSqlCollection.Name)]
public class when_provisioning_truncated_scoped_constraints_on_postgresql(PostgreSqlFixture fixture) : for_Database.given.a_postgresql_database(fixture)
{
    readonly SchemaScope _scope = new(["a-store"], ["a-namespace"]);
    SchemaReport _stale;
    SchemaReport _current;
    bool _readOnly;
    bool _unrelatedUnchanged;
    string _claims;
    string _values;

    async Task Establish()
    {
        await new SchemaProvisioner(Database, NullLogger<SchemaProvisioner>.Instance).Migrate(_scope);
        await using (var registry = await Database.EventStore(Store))
        {
            registry.DbContext.Constraints.Add(new UniqueConstraintDefinition("scoped_claim", [], Scope: new("scoped")).ToSql(1));
            registry.DbContext.Constraints.Add(new UniqueConstraintDefinition("scoped_value", [], Scope: new("scoped")) { Mode = UniqueConstraintMode.PerValue }.ToSql(1));
            await registry.DbContext.SaveChangesAsync();
        }
        var scopeKey = new string('s', 80);
        var valuesScopeKey = new string('s', 63 - "event-log_scoped_value_".Length - "_constraint".Length);
        await using (var claims = await Database.UniqueConstraintTable(Store, Namespace, $"event-log_scoped_claim_{scopeKey}_constraint")) await claims.DbContext.EnsureTableExists();
        await using (var values = await Database.UniqueConstraintValuesTable(Store, Namespace, $"event-log_scoped_value_{valuesScopeKey}_constraint_values")) await values.DbContext.EnsureTableExists();
        await using var ns = await Database.Namespace(Store, Namespace);
        var tables = await SchemaTables.Read(ns.DbContext);
        _claims = tables.Keys.Single(name => name.StartsWith("event-log_scoped_claim_", StringComparison.Ordinal));
        _values = tables.Keys.Single(name => name.StartsWith("event-log_scoped_value_", StringComparison.Ordinal));
#pragma warning disable EF1002 // The identifiers are read from this fixture's catalog and contain only fixed ASCII characters.
        await ns.DbContext.Database.ExecuteSqlRawAsync($"ALTER TABLE \"{_claims}\" DROP COLUMN \"Subject\"; ALTER TABLE \"{_values}\" DROP COLUMN \"Subject\"");
#pragma warning restore EF1002
        await ns.DbContext.Database.ExecuteSqlRawAsync("CREATE TABLE \"event-log_scoped_claim_unrelated_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\" (\"Value\" TEXT, \"EventSourceId\" TEXT, \"SequenceNumber\" NUMERIC PRIMARY KEY); CREATE TABLE \"event-log_unregistered_xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\" (\"Value\" TEXT, \"EventSourceId\" TEXT PRIMARY KEY, \"SequenceNumber\" NUMERIC)");
    }

    async Task Because()
    {
        var provisioner = new SchemaProvisioner(Database, NullLogger<SchemaProvisioner>.Instance);
        var before = await Snapshot();
        _stale = await provisioner.Verify(_scope);
        var afterVerification = await Snapshot();
        _readOnly = before.SequenceEqual(afterVerification, StringComparer.Ordinal);
        await provisioner.Migrate(_scope);
        await provisioner.Migrate(_scope);
        _current = await provisioner.Verify(_scope);
        await using var ns = await Database.Namespace(Store, Namespace);
        var tables = await SchemaTables.Read(ns.DbContext);
        var unrelated = tables.Where(table => table.Key.StartsWith("event-log_scoped_claim_unrelated_", StringComparison.Ordinal) || table.Key.StartsWith("event-log_unregistered_", StringComparison.Ordinal)).ToArray();
        _unrelatedUnchanged = unrelated.Length == 2 && unrelated.All(table => !table.Value.Contains("Subject", StringComparer.Ordinal));
    }

    async Task<string[]> Snapshot()
    {
        await using var ns = await Database.Namespace(Store, Namespace);
        return await ns.DbContext.Database.SqlQuery<string>($"SELECT table_name || ':' || column_name || ':' || data_type AS \"Value\" FROM information_schema.columns WHERE table_schema = current_schema() UNION ALL SELECT tablename || ':' || indexname || ':' || indexdef AS \"Value\" FROM pg_indexes WHERE schemaname = current_schema() ORDER BY \"Value\"").ToArrayAsync();
    }

    [Fact] void should_cover_a_values_name_truncated_to_a_claims_suffix() => _values.EndsWith("_constraint", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_truncated_claim_table_as_stale() => _stale.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains(_claims, StringComparison.Ordinal) && item.Contains("Subject", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_truncated_values_table_as_stale() => _stale.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains(_values, StringComparison.Ordinal) && item.Contains("Subject", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_leave_the_catalog_unchanged_during_verification() => _readOnly.ShouldBeTrue();
    [Fact] void should_repair_both_tables_idempotently() => _current.Scopes.SelectMany(scope => scope.Pending).ShouldBeEmpty();
    [Fact] void should_not_migrate_unregistered_or_wrong_primary_key_tables() => _unrelatedUnchanged.ShouldBeTrue();
}
