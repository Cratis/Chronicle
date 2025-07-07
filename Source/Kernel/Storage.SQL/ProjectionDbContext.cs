// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.SQL;

/// <summary>
/// Represents the DbContext for projection data storage.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProjectionDbContext"/> class.
/// </remarks>
/// <param name="options">The <see cref="DbContextOptions"/> for the context.</param>
public class ProjectionDbContext(DbContextOptions<ProjectionDbContext> options) : DbContext(options)
{
    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // We'll configure the model dynamically based on projection schemas
        // For now, this is left empty as tables will be created dynamically
    }
}