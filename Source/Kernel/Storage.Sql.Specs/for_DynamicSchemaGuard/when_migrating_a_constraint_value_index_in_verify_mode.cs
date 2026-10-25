// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_migrating_a_constraint_value_index_in_verify_mode : given.a_constraint_index
{
    bool _unique;
    int _logsAfterFirst;
    int _logsAfterSecond;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_guarded_claims_Value ON guarded_claims (Value)");

    async Task Because()
    {
        await EnsureIndex();
        _unique = await DynamicIndexShape.IsUniqueValue(_context, Table, Index);
        _logsAfterFirst = _guardLogger.Entries.Count;
        await EnsureIndex();
        _logsAfterSecond = _guardLogger.Entries.Count;
    }

    [Fact] void should_replace_the_non_unique_index() => _unique.ShouldBeTrue();
    [Fact] void should_warn_about_runtime_ddl() => _guardLogger.Entries.Count(entry => entry.Level == LogLevel.Warning).ShouldEqual(1);
    [Fact] void should_not_repeat_the_warning_for_a_current_index() => _logsAfterSecond.ShouldEqual(_logsAfterFirst);
}
