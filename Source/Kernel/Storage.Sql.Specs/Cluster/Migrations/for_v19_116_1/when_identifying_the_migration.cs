// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations.for_v19_116_1;

public class when_identifying_the_migration : Specification
{
    const string Id = "Cluster-ResourceNameReservations-v19_116_1";
    string _id;
    Type _context;
    string[] _ordered;

    void Because()
    {
        _id = typeof(v19_116_1).GetCustomAttribute<MigrationAttribute>()!.Id;
        _context = typeof(v19_116_1).GetCustomAttribute<DbContextAttribute>()!.ContextType;
        _ordered = new[] { Id, "Cluster-AdministratorPrivileges-v19_112_0_2", "Cluster-SingleWriterLease-v19_105_0" }.Order(MigrationReleaseOrder.Instance).ToArray();
    }

    [Fact] void should_have_the_expected_id() => _id.ShouldEqual(Id);
    [Fact] void should_belong_to_the_cluster_context() => _context.ShouldEqual(typeof(ClusterDbContext));
    [Fact] void should_follow_the_administrator_migration() => _ordered[^1].ShouldEqual(Id);
}
