// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

public class and_a_read_model_target_changes_after_admission : given.a_database_without_read_model_storage
{
    Exception? _runtime;
    Exception? _schema;
    Exception? _existing;
    bool _createdRelocatedDatabase;
    string _catalog;

    async Task Because()
    {
        var admitted = Path.Combine(_directory, "admitted.db");
        var relocated = Path.Combine(_directory, "relocated.db");
        Resolver.Answer = _ => new ResolvedConnection(Database: admitted);
        var database = ResolvingDatabase();
        var provenance = new SinkProvenanceStorage(database, "store", "namespace");
        await provenance.CompleteInventory(false);
        var target = provenance.Target!;
        Resolver.Answer = request => new ResolvedConnection(Database: request.Purpose == ConnectionPurpose.Runtime ? relocated : admitted);
        _runtime = await Catch.Exception(async () =>
        {
            await using var scope = await database.ReadModelTable("store", "namespace", "payload", _columns, admittedTarget: target);
        });
        Resolver.Answer = request => new ResolvedConnection(Database: request.Purpose == ConnectionPurpose.Schema ? relocated : admitted);
        _schema = await Catch.Exception(async () =>
        {
            await using var scope = await database.ReadModelTable("store", "namespace", "payload", _columns, admittedTarget: target);
        });
        _existing = await Catch.Exception(async () =>
        {
            await using var scope = await database.ExistingReadModelTable("store", "namespace", "payload", _columns, target);
        });
        _createdRelocatedDatabase = File.Exists(relocated);
        _catalog = await Catalog($"DataSource={admitted}");
    }

    [Fact] void should_deny_a_changed_actual_runtime_target() => _runtime.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_deny_a_changed_actual_schema_preparation_target() => _schema.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_guard_non_provisioning_probes_too() => _existing.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_create_the_unadmitted_database() => _createdRelocatedDatabase.ShouldBeFalse();
    [Fact] void should_not_prepare_the_payload_table() => _catalog.ShouldNotContain("payload");
}
