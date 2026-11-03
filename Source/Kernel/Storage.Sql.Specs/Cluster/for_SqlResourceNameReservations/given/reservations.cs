// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Naming;
using Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;
using Microsoft.EntityFrameworkCore;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceNameReservations.given;

public class reservations : Specification
{
    protected LeaseHarness Harness;
    protected IResourceNameReservations Reservations;

    async Task Establish()
    {
        Harness = new LeaseHarness();
        await Harness.Initialize();
        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(Harness.CreateContext(), () => { })));
        Reservations = new SqlResourceNameReservations(database);
    }

    async Task Destroy() => await Harness.DisposeAsync();

    protected async Task<List<ResourceNameReservationEntity>> Rows()
    {
        await using var context = Harness.CreateContext();
        return await context.ResourceNameReservations.AsNoTracking().ToListAsync();
    }
}
