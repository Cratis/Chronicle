// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable.given;

public abstract class a_membership_table(MembershipHarness harness) : Specification
{
    protected const string ClusterId = "membership-spec";
    protected readonly MembershipHarness Harness = harness;
    protected SqlMembershipTable Table;
    protected MembershipEntry Entry;
    protected IDatabase Database;

    protected virtual bool NonDefaultSchema => false;

    async Task Establish()
    {
        await Harness.Initialize(NonDefaultSchema);
        Database = Substitute.For<IDatabase>();
        Database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(Harness.CreateContext(), () => { })));
        Table = new SqlMembershipTable(Database, Options.Create(new ClusterOptions { ClusterId = ClusterId }));
        await Table.InitializeMembershipTable(true);
        Entry = CreateEntry(1);
    }

    protected SqlMembershipTable WithInterceptors(params IInterceptor[] interceptors)
    {
        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(Harness.CreateContext(interceptors), () => { })));
        return new SqlMembershipTable(database, Options.Create(new ClusterOptions { ClusterId = ClusterId }));
    }

    protected static MembershipEntry CreateEntry(int generation) => new()
    {
        SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.IPv6Loopback, 11111), generation),
        SiloName = "silo",
        HostName = "host",
        Status = SiloStatus.Joining,
        ProxyPort = 30000,
        RoleName = "role",
        UpdateZone = 2,
        FaultZone = 3,
        StartTime = new DateTime(2026, 1, 2, 3, 4, 5, 123, DateTimeKind.Utc),
        IAmAliveTime = new DateTime(2026, 1, 2, 3, 4, 6, 456, DateTimeKind.Utc),
        SuspectTimes = [Tuple.Create(SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11112), 2), new DateTime(2026, 1, 2, 3, 4, 7, 987, DateTimeKind.Utc))]
    };

    protected async Task<MembershipTableData> Insert()
    {
        var current = await Table.ReadAll();
        (await Table.InsertRow(Entry, current.Version.Next())).ShouldBeTrue();
        return await Table.ReadRow(Entry.SiloAddress);
    }

    async Task Destroy() => await Harness.DisposeAsync();
}
