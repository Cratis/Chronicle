// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_DbUpdateExceptionExtensions.when_classifying_a_failed_write;

public class and_sqlite_rejected_a_duplicate_primary_key : Specification
{
    DbUpdateException _exception;
    bool _result;

    void Establish() => _exception = new DbUpdateException("The write failed.", new SqliteException("UNIQUE constraint failed", 19, 1555));

    void Because() => _result = _exception.IsUniqueViolation();

    [Fact] void should_be_a_unique_violation() => _result.ShouldBeTrue();
}
