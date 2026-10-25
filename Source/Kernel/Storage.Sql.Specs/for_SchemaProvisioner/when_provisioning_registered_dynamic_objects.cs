// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Data.Sqlite;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_provisioning_registered_dynamic_objects : given.a_schema_provisioner
{
    bool _objectsExist;
    bool _migrationWasIdempotent;
    bool _verificationWasReadOnly;
    SchemaReport _current;
    SchemaReport _damaged;
    SchemaReport _stillDamaged;

    async Task Establish()
    {
        await _provisioner.Migrate(Scope);
        await given.RegisteredDynamicSchema.Register(_schemaDatabase, "orders", "production");
    }

    async Task Because()
    {
        await _provisioner.Migrate(Scope);
        _objectsExist = await given.RegisteredDynamicSchema.AllObjectsExist(_schemaDatabase, "orders", "production");
        var provisioned = await Snapshot();
        await _provisioner.Migrate(Scope);
        var reprovisioned = await Snapshot();
        _migrationWasIdempotent = provisioned.SequenceEqual(reprovisioned, StringComparer.Ordinal);
        _current = await _provisioner.Verify(Scope);
        await given.RegisteredDynamicSchema.BreakIndexes(_schemaDatabase, "orders", "production");
        var broken = await Snapshot();
        _damaged = await _provisioner.Verify(Scope);
        _stillDamaged = await _provisioner.Verify(Scope);
        var verified = await Snapshot();
        _verificationWasReadOnly = broken.SequenceEqual(verified, StringComparer.Ordinal);
    }

    async Task<string[]> Snapshot()
    {
        var result = new List<string>();
        foreach (var file in Directory.GetFiles(_directory, "*.db").Order(StringComparer.Ordinal))
        {
            await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = file, Mode = SqliteOpenMode.ReadOnly, Pooling = false }.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "PRAGMA schema_version";
            result.Add($"{Path.GetFileName(file)}:schema-version:{await command.ExecuteScalarAsync()}");
            command.CommandText = "SELECT type || ':' || name || ':' || COALESCE(sql, '') FROM sqlite_master WHERE name NOT LIKE 'sqlite_%' ORDER BY type, name";
            await using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync()) result.Add($"{Path.GetFileName(file)}:{reader.GetString(0)}");
        }
        return result.ToArray();
    }

    [Fact] void should_create_registered_and_catalog_discovered_objects() => _objectsExist.ShouldBeTrue();
    [Fact] void should_not_run_schema_changing_ddl_on_a_second_migration() => _migrationWasIdempotent.ShouldBeTrue();
    [Fact] void should_verify_the_complete_manifest() => _current.IsCurrent.ShouldBeTrue();
    [Fact] void should_aggregate_the_missing_constraint_index() => _damaged.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains(given.RegisteredDynamicSchema.RegisteredValueIndex, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_also_report_the_missing_declared_read_model_index() => _damaged.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains("declared index", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_leave_verification_read_only() => _verificationWasReadOnly.ShouldBeTrue();
    [Fact] void should_report_the_same_damage_on_the_next_verification() => _stillDamaged.Scopes.SelectMany(scope => scope.Pending).SequenceEqual(_damaged.Scopes.SelectMany(scope => scope.Pending), StringComparer.Ordinal).ShouldBeTrue();
}
