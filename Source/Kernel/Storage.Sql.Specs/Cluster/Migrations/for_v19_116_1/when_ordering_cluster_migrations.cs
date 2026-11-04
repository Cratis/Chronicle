// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Cratis.Chronicle.Storage.Sql.Cluster.Migrations.for_v19_116_1;

public class when_ordering_cluster_migrations : Specification
{
    string[] _ordered;

    void Because() =>
        _ordered =
        [
            .. typeof(v19_116_1).Assembly.GetTypes()
                .Where(_ => _.GetCustomAttribute<DbContextAttribute>()?.ContextType == typeof(ClusterDbContext))
                .Select(_ => _.GetCustomAttribute<MigrationAttribute>()!.Id)
                .Order(MigrationReleaseOrder.Instance)
        ];

    [Fact] void should_sort_after_the_administrator_migration() =>
        _ordered.ToList().IndexOf("Cluster-ResourceNameReservations-v19_116_1").ShouldBeGreaterThan(_ordered.ToList().IndexOf("Cluster-AdministratorPrivileges-v19_112_0_2"));

    [Fact] void should_be_the_last_cluster_migration() => _ordered[^1].ShouldEqual("Cluster-ResourceNameReservations-v19_116_1");
}
