// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_databases_must_be_preprovisioned : given.a_schema_provisioner
{
    Exception _exception;

    void Establish() => File.Delete(Path.Combine(_directory, "cluster.db"));

    async Task Because() => _exception = await Catch.Exception(() => CreateServices(SchemaMode.Migrate, false)
        .GetRequiredService<ISchemaProvisioner>().Migrate(Scope));

    [Fact] void should_preserve_the_creation_policy() => _exception.ShouldBeOfExactType<DatabaseDoesNotExist>();
    [Fact] void should_not_create_a_database() => Directory.GetFiles(_directory).ShouldBeEmpty();
}
