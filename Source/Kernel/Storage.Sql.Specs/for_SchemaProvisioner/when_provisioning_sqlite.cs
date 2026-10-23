// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_provisioning_sqlite : given.a_schema_provisioner
{
    SchemaReport _first;
    SchemaReport _second;
    SchemaReport _verified;
    string[] _stores;
    string[] _namespaces;

    async Task Because()
    {
        _first = await _provisioner.Migrate(Scope);
        _second = await _provisioner.Migrate(Scope);
        _verified = await _provisioner.Verify(Scope);
        await using var cluster = await _schemaDatabase.Cluster();
        _stores = await cluster.DbContext.EventStores.Select(store => store.Name).ToArrayAsync();
        await using var store = await _schemaDatabase.EventStore("orders");
        _namespaces = await store.DbContext.Namespaces.Select(ns => ns.Name).ToArrayAsync();
    }

    [Fact] void should_provision_all_scopes() => _first.IsCurrent.ShouldBeTrue();
    [Fact] void should_be_idempotent() => _second.IsCurrent.ShouldBeTrue();
    [Fact] void should_verify_the_provisioned_schema() => _verified.IsCurrent.ShouldBeTrue();
    [Fact] void should_include_jobs() => _verified.Scopes.Any(scope => scope.Scope.Contains("JobsDbContext", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_register_the_explicit_store() => _stores.ShouldContain("orders");
    [Fact] void should_register_the_explicit_namespace() => _namespaces.ShouldContain("production");
}
