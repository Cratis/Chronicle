// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

[Collection(SqlServerCollection.Name)]
public class when_provisioning_registered_dynamic_objects_on_sql_server(SqlServerFixture fixture) : for_Database.given.a_sql_server_database(fixture)
{
    bool _objectsExist;
    bool _objectsRemainMissing;
    SchemaReport _current;
    SchemaReport _damaged;
    SchemaReport _stillDamaged;

    async Task Because()
    {
        await using var services = given.schema_services.Create(new ChronicleOptions
        {
            Storage = new() { Type = StorageType.MsSql, ConnectionDetails = ConnectionString }
        });
        var database = services.GetRequiredService<Database>();
        var provisioner = services.GetRequiredService<ISchemaProvisioner>();
        var scope = new SchemaScope([Store], ["production"]);
        await provisioner.Migrate(scope);
        await given.RegisteredDynamicSchema.Register(database, Store, "production");
        await provisioner.Migrate(scope);
        await provisioner.Migrate(scope);
        _objectsExist = await given.RegisteredDynamicSchema.AllObjectsExist(database, Store, "production");
        _current = await provisioner.Verify(scope);
        await given.RegisteredDynamicSchema.BreakIndexes(database, Store, "production");
        _damaged = await provisioner.Verify(scope);
        _stillDamaged = await provisioner.Verify(scope);
        _objectsRemainMissing = !await given.RegisteredDynamicSchema.AllObjectsExist(database, Store, "production");
    }

    [Fact] void should_provision_registered_and_discovered_objects() => _objectsExist.ShouldBeTrue();
    [Fact] void should_verify_after_repeated_migration() => _current.Scopes.SelectMany(scope => scope.Pending).ShouldBeEmpty();
    [Fact] void should_report_the_missing_constraint_index() => _damaged.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains(given.RegisteredDynamicSchema.RegisteredValueIndex, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_also_report_the_missing_declared_index() => _damaged.Scopes.SelectMany(scope => scope.Pending).Any(item => item.Contains("declared index", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_repair_damaged_objects_during_verification() => _objectsRemainMissing.ShouldBeTrue();
    [Fact] void should_report_the_same_damage_again() => _stillDamaged.Scopes.SelectMany(scope => scope.Pending).SequenceEqual(_damaged.Scopes.SelectMany(scope => scope.Pending), StringComparer.Ordinal).ShouldBeTrue();
}
