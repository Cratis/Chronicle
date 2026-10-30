// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.SingleWriter;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlSingleWriterLeaseStorage.given;

public class a_lease(LeaseHarness harness) : Specification
{
    protected readonly LeaseHarness Harness = harness;
    protected ISingleWriterLeaseStorage Storage;
    protected SingleWriterLeaseClaim Claim;

    async Task Establish()
    {
        await Harness.Initialize();
        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(Harness.CreateContext(), () => { })));
        Storage = new SqlSingleWriterLeaseStorage(database);
        Claim = new(new("Owner-A"), TimeSpan.FromSeconds(30), "cluster", "service", "host", "1.0");
    }

    async Task Destroy() => await Harness.DisposeAsync();
}
