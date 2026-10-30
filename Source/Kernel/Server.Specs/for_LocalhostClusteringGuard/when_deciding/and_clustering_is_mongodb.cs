// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Server.for_LocalhostClusteringGuard.when_deciding;

public class and_clustering_is_mongodb : Specification
{
    LocalhostClusteringGuardAction _result;

    void Because() => _result = LocalhostClusteringGuard.Decide(new ChronicleOptions
    {
        Storage = new() { Type = "MongoDB", ConnectionDetails = "mongodb://shared-db" },
        Clustering = new() { Type = ClusteringType.MongoDB, SingleWriterLease = new() { Enabled = true } }
    });

    [Fact] void should_select_the_guard() => _result.ShouldEqual(LocalhostClusteringGuardAction.None);
}
