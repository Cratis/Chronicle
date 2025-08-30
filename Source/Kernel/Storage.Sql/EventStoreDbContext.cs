// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql;

/// <summary>
/// Represents the database context for an event store.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class EventStoreDbContext(DbContextOptions<EventStoreDbContext> options) : DbContext(options);
