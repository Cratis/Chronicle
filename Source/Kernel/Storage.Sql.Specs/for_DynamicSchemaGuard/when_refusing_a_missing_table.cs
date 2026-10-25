// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_refusing_a_missing_table : given.a_dynamic_table
{
    Exception _exception;
    bool _exists;

    async Task Because()
    {
        using var policy = DynamicSchemaGuard.Use(DynamicSchemaPolicy.Refuse);
        _exception = await Catch.Exception(Ensure);
        _exists = (await SchemaTables.Read(_context)).ContainsKey(_table);
    }

    [Fact] void should_throw_the_provisioning_exception() => _exception.ShouldBeOfExactType<DynamicSchemaObjectNotProvisioned>();
    [Fact] void should_not_execute_the_create_delegate() => _creates.ShouldEqual(0);
    [Fact] void should_not_create_the_table() => _exists.ShouldBeFalse();
    [Fact] void should_not_warn_about_a_creation() => Logs(LogLevel.Warning).ShouldEqual(0);
    [Fact] void should_name_the_migrate_only_command() => _exception.Message.ShouldContain("Run chronicle --migrate-only");
}
