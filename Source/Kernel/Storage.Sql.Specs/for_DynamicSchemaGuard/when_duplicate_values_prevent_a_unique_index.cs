// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DynamicSchemaGuard;

public class when_duplicate_values_prevent_a_unique_index : given.a_constraint_index
{
    Exception? _exception;
    bool _indexExists;
    bool _unique;

    async Task Establish() => await _context.Database.ExecuteSqlRawAsync("INSERT INTO guarded_claims VALUES ('first', 'duplicate'), ('second', 'duplicate')");

    async Task Because()
    {
        _exception = await Catch.Exception(EnsureIndex);
        _indexExists = await DynamicIndexShape.Exists(_context, Table, Index);
        _unique = await DynamicIndexShape.IsUniqueValue(_context, Table, Index);
    }

    [Fact] void should_preserve_the_non_unique_fallback() => _indexExists.ShouldBeTrue();
    [Fact] void should_not_claim_that_the_fallback_is_unique() => _unique.ShouldBeFalse();
    [Fact] void should_allow_runtime_to_continue() => _exception.ShouldBeNull();
}
