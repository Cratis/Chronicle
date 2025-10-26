// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Applications.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces;

/// <summary>
/// Represents the DbContext for an event store namespace.
/// </summary>
/// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
public class NamespaceDbContext(DbContextOptions<NamespaceDbContext> options) : BaseDbContext(options)
{
    /// <summary>
    /// Gets or sets the observer state DbSet.
    /// </summary>
    public DbSet<Observers.ObserverState> Observers { get; set; } = null!;
}
