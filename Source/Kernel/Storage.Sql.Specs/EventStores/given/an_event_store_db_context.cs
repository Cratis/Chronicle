// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.EntityFrameworkCore.Concepts;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.EventStores.given;

/// <summary>
/// Sets up an in-memory SQLite <see cref="EventStoreDbContext"/> with ConceptAs support enabled.
/// </summary>
public class an_event_store_db_context : Specification
{
    protected EventStoreDbContext context;

    void Establish()
    {
        var options = new DbContextOptionsBuilder<EventStoreDbContext>()
            .UseSqlite("DataSource=:memory:")
            .AddConceptAsSupport()
            .Options;

        context = new EventStoreDbContext(options);
        context.Database.OpenConnection();
        context.Database.EnsureCreated();
    }

    void Destroy()
    {
        context.Database.CloseConnection();
        context.Dispose();
    }
}
