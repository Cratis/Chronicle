// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

public class SchemaLockDbContext(DbContextOptions<SchemaLockDbContext> options) : DbContext(options)
{
    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // The spec project builds more than twenty distinct EF option sets, and EF Core throws for any
        // new one created after the twentieth; which one that is depends on test order.
        optionsBuilder.ConfigureWarnings(warnings => warnings.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning));
    }
}
