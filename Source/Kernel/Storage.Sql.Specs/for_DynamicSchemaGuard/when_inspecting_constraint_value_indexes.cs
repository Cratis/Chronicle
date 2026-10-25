// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_inspecting_constraint_value_indexes : given.a_constraint_index
{
    Exception? _missing;
    Exception? _wrong;
    Exception? _current;
    bool _missingRemains;
    bool _wrongRemains;

    async Task Because()
    {
        _missing = await Inspect();
        _missingRemains = !await DynamicIndexShape.Exists(_context, Table, Index);
        await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_guarded_claims_Value ON guarded_claims (Value)");
        _wrong = await Inspect();
        _wrongRemains = await DynamicIndexShape.Exists(_context, Table, Index) && !await DynamicIndexShape.IsUniqueValue(_context, Table, Index);
        await _context.Database.ExecuteSqlRawAsync("DROP INDEX IX_guarded_claims_Value");
        await _context.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX IX_guarded_claims_Value ON guarded_claims (Value)");
        _current = await Inspect();
    }

    [Fact] void should_report_a_missing_index() => _missing.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_leave_the_missing_index_absent() => _missingRemains.ShouldBeTrue();
    [Fact] void should_report_a_non_unique_index() => _wrong.ShouldBeOfExactType<SchemaNotCurrent>();
    [Fact] void should_preserve_the_non_unique_index() => _wrongRemains.ShouldBeTrue();
    [Fact] void should_accept_a_matching_unique_index() => _current.ShouldBeNull();
    [Fact] void should_not_log_runtime_ddl_during_inspection() => _guardLogger.Entries.Count.ShouldEqual(0);
}
