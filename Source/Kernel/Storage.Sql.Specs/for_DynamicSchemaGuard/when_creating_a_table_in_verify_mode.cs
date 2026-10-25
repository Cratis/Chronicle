// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_creating_a_table_in_verify_mode : given.a_dynamic_table
{
    bool _exists;

    async Task Because()
    {
        await Ensure();
        await Ensure();
        _exists = (await SchemaTables.Read(_context)).ContainsKey(_table);
    }

    [Fact] void should_create_the_table() => _exists.ShouldBeTrue();
    [Fact] void should_not_recreate_an_already_current_table() => _creates.ShouldEqual(1);
    [Fact] void should_report_the_object_once_at_debug() => Logs(LogLevel.Debug).ShouldEqual(1);
    [Fact] void should_emit_one_summary_warning() => Logs(LogLevel.Warning).ShouldEqual(1);
}
