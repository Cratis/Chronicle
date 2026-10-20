// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Orleans.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlMembershipTable;

public class when_using_sqlite : Specification
{
    SqlMembershipTable _table;
    MembershipEntry _entry;

    void Establish()
    {
        var options = new DbContextOptionsBuilder<ClusterDbContext>().UseSqlite("Data Source=:memory:").Options;
        var database = Substitute.For<IDatabase>();
        database.Cluster().Returns(_ => Task.FromResult(new DbContextScope<ClusterDbContext>(new ClusterDbContext(options), () => { })));
        _table = new SqlMembershipTable(database, Options.Create(new ClusterOptions { ClusterId = "sqlite-spec" }));
        _entry = new MembershipEntry { SiloAddress = SiloAddress.New(new IPEndPoint(IPAddress.Loopback, 11111), 1) };
    }

    [Fact] public async Task should_refuse_initialization() => (await Catch.Exception(() => _table.InitializeMembershipTable(true))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_initialization_without_a_version() => (await Catch.Exception(() => _table.InitializeMembershipTable(false))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_reading_all() => (await Catch.Exception(_table.ReadAll)).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_reading_a_row() => (await Catch.Exception(() => _table.ReadRow(_entry.SiloAddress))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_inserting() => (await Catch.Exception(() => _table.InsertRow(_entry, new TableVersion(1, "0")))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_updating() => (await Catch.Exception(() => _table.UpdateRow(_entry, "0", new TableVersion(1, "0")))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_updating_the_heartbeat() => (await Catch.Exception(() => _table.UpdateIAmAlive(_entry))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_cleanup() => (await Catch.Exception(() => _table.CleanupDefunctSiloEntries(DateTimeOffset.UtcNow))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
    [Fact] public async Task should_refuse_deleting() => (await Catch.Exception(() => _table.DeleteMembershipTableEntries("sqlite-spec"))).ShouldBeOfExactType<SqlMembershipRequiresServerDatabase>();
}
