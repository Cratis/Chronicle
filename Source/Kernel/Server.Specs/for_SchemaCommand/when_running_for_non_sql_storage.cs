// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.Sql;
using Cratis.Chronicle.Storage.Sql.Schema;

namespace Cratis.Chronicle.Server.for_SchemaCommand;

public class when_running_for_non_sql_storage : Specification
{
    int _exit;

    async Task Because() => _exit = await SchemaCommandRunner.Run(
        SchemaCommand.Parse(["--migrate-only"]),
        new ChronicleOptions { Storage = new() { Type = StorageType.InMemory } },
        SchemaAction.MigrateAndExit);

    [Fact] void should_exit_successfully_without_a_host() => _exit.ShouldEqual(0);
    [Fact] void should_map_missing_schema_to_two() => SchemaCommandRunner.ExitCode(new SchemaNotCurrent("cluster", "cluster", ["pending"]), SchemaAction.VerifyAndExit).ShouldEqual(2);
    [Fact] void should_map_missing_database_to_four() => SchemaCommandRunner.ExitCode(new DatabaseDoesNotExist("cluster"), SchemaAction.MigrateAndExit).ShouldEqual(4);
    [Fact] void should_map_unexpected_errors_to_one() => SchemaCommandRunner.ExitCode(new IOException("failure"), SchemaAction.MigrateAndExit).ShouldEqual(1);
}
