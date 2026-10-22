// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.for_ReleaseOrderedMigrator.given;

public class ReleasesDbContext(DbContextOptions<ReleasesDbContext> options) : DbContext(options);
