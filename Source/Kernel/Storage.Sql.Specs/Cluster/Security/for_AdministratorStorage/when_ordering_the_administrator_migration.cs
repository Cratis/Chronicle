// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.Security.for_AdministratorStorage;

public class when_ordering_the_administrator_migration : Specification
{
    string[] _ordered;

    void Because() => _ordered = new[]
    {
        "Cluster-AdministratorPrivileges-v19_112_0_2",
        "Cluster-SingleWriterLease-v19_105_0",
        "Cluster-OpenIddictApplications-v19_93_0"
    }.Order(MigrationReleaseOrder.Instance).ToArray();

    [Fact] void should_follow_the_single_writer_lease_migration() => _ordered[^1].ShouldEqual("Cluster-AdministratorPrivileges-v19_112_0_2");
}
