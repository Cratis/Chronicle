// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterMembershipConfiguration;

public class when_membership_settings_are_omitted : Specification
{
    ClusterMembershipOptions _options;
    ClusterMembershipOptions _defaults;

    void Establish()
    {
        _options = new();
        _defaults = new();
    }

    void Because()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["clustering:type"] = "Sql"
        }).Build();
        var clustering = configuration.GetSection("clustering").Get<Clustering>();
        ClusterMembershipConfiguration.Apply(clustering!, _options);
    }

    [Fact] void should_preserve_apply_the_probe_timeout() => _options.ProbeTimeout.ShouldEqual(_defaults.ProbeTimeout);
    [Fact] void should_preserve_apply_the_missed_probe_limit() => _options.NumMissedProbesLimit.ShouldEqual(_defaults.NumMissedProbesLimit);
    [Fact] void should_preserve_apply_the_death_vote_threshold() => _options.NumVotesForDeathDeclaration.ShouldEqual(_defaults.NumVotesForDeathDeclaration);
    [Fact] void should_preserve_apply_the_table_refresh_interval() => _options.TableRefreshTimeout.ShouldEqual(_defaults.TableRefreshTimeout);
    [Fact] void should_preserve_apply_the_heartbeat_publish_interval() => _options.IAmAliveTablePublishTimeout.ShouldEqual(_defaults.IAmAliveTablePublishTimeout);
    [Fact] void should_preserve_apply_the_probed_silo_count() => _options.NumProbedSilos.ShouldEqual(_defaults.NumProbedSilos);
    [Fact] void should_keep_the_cleanup_default() => _options.DefunctSiloCleanupPeriod.ShouldEqual(TimeSpan.FromHours(1));
    [Fact] void should_keep_the_expiration_default() => _options.DefunctSiloExpiration.ShouldEqual(TimeSpan.FromHours(3));
}
