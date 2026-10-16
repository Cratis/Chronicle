// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Connections;

/// <summary>
/// Represents <see cref="DbContextOptions{TContext}"/> kept for the connection string they were built for.
/// </summary>
/// <typeparam name="TContext">The type of <see cref="DbContext"/>.</typeparam>
/// <param name="ConnectionString">The connection string the options were built for.</param>
/// <param name="Options">The options.</param>
internal sealed record CachedDbContextOptions<TContext>(string ConnectionString, DbContextOptions<TContext> Options)
    where TContext : DbContext;
