// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner;

public class when_runtime_verify_finds_a_missing_database : given.a_schema_provisioner
{
    Exception _exception;

    void Establish() => File.Delete(Path.Combine(_directory, "cluster.db"));

    async Task Because()
    {
        var database = CreateServices(SchemaMode.Verify).GetRequiredService<Database>();
        _exception = await Catch.Exception(async () =>
        {
            await using var scope = await database.Cluster();
        });
    }

    [Fact] void should_fail_with_an_actionable_exception() => _exception.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_recommend_the_migration_command() => _exception.Message.ShouldContain("chronicle --migrate-only");
    [Fact] void should_not_create_a_database() => Directory.GetFiles(_directory).ShouldBeEmpty();
}
