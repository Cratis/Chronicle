// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_refusing_a_constraint_value_index_upgrade : given.a_constraint_index
{
    Exception? _exception;
    bool _originalRemains;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("CREATE INDEX IX_guarded_claims_Value ON guarded_claims (Value)");

    async Task Because()
    {
        using (DynamicSchemaGuard.Use(DynamicSchemaPolicy.Refuse))
        {
            _exception = await Catch.Exception(EnsureIndex);
        }
        _originalRemains = await DynamicIndexShape.Exists(_context, Table, Index) && !await DynamicIndexShape.IsUniqueValue(_context, Table, Index);
    }

    [Fact] void should_refuse_before_ddl() => _exception.ShouldBeOfExactType<DynamicSchemaObjectNotProvisioned>();
    [Fact] void should_preserve_the_existing_non_unique_index() => _originalRemains.ShouldBeTrue();
    [Fact] void should_not_log_runtime_ddl() => _guardLogger.Entries.Count.ShouldEqual(0);
}
