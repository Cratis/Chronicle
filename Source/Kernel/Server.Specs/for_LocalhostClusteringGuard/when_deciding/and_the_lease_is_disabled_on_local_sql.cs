// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_LocalhostClusteringGuard.when_deciding;

public class and_the_lease_is_disabled_on_local_sql : Specification
{
    LocalhostClusteringGuardAction _result;

    void Because() => _result = LocalhostClusteringGuard.Decide(new ChronicleOptions
    {
        Storage = new() { Type = "MsSql", ConnectionDetails = "Server=localhost;Database=chronicle" },
        Clustering = new() { Type = ClusteringType.Localhost, SingleWriterLease = new() { Enabled = false } }
    });

    [Fact] void should_select_the_guard() => _result.ShouldEqual(LocalhostClusteringGuardAction.None);
}
