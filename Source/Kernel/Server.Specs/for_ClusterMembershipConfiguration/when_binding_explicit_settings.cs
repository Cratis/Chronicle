// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterMembershipConfiguration;

public class when_binding_explicit_settings : Specification
{
    ClusterMembershipOptions _options;

    void Because()
    {
        var values = new Dictionary<string, string?>
        {
            ["clustering:probeTimeout"] = "00:00:02",
            ["clustering:numMissedProbesLimit"] = "4",
            ["clustering:numVotesForDeathDeclaration"] = "3",
            ["clustering:tableRefreshTimeout"] = "00:00:07",
            ["clustering:iAmAliveTablePublishTimeout"] = "00:00:11",
            ["clustering:numProbedSilos"] = "5",
            ["clustering:defunctSiloCleanupPeriod"] = "00:20:00",
            ["clustering:defunctSiloExpiration"] = "02:00:00"
        };
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var clustering = configuration.GetSection("clustering").Get<Clustering>();
        _options = new();
        ClusterMembershipConfiguration.Apply(clustering!, _options);
    }

    [Fact] void should_apply_the_probe_timeout() => _options.ProbeTimeout.ShouldEqual(TimeSpan.FromSeconds(2));
    [Fact] void should_apply_the_missed_probe_limit() => _options.NumMissedProbesLimit.ShouldEqual(4);
    [Fact] void should_apply_the_death_vote_threshold() => _options.NumVotesForDeathDeclaration.ShouldEqual(3);
    [Fact] void should_apply_the_table_refresh_interval() => _options.TableRefreshTimeout.ShouldEqual(TimeSpan.FromSeconds(7));
    [Fact] void should_apply_the_heartbeat_publish_interval() => _options.IAmAliveTablePublishTimeout.ShouldEqual(TimeSpan.FromSeconds(11));
    [Fact] void should_apply_the_probed_silo_count() => _options.NumProbedSilos.ShouldEqual(5);
    [Fact] void should_keep_the_cleanup_binding() => _options.DefunctSiloCleanupPeriod.ShouldEqual(TimeSpan.FromMinutes(20));
    [Fact] void should_keep_the_expiration_binding() => _options.DefunctSiloExpiration.ShouldEqual(TimeSpan.FromHours(2));
}
