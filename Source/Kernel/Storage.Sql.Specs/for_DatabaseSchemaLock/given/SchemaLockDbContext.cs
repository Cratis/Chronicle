// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.given;

public class SchemaLockDbContext(DbContextOptions<SchemaLockDbContext> options) : DbContext(options);
