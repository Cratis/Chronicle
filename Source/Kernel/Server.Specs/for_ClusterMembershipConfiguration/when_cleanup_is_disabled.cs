// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterMembershipConfiguration;

public class when_cleanup_is_disabled : Specification
{
    ClusterMembershipOptions _options;

    void Because()
    {
        _options = new();
        ClusterMembershipConfiguration.Apply(new Clustering { DefunctSiloCleanupPeriod = TimeSpan.Zero }, _options);
    }

    [Fact] void should_disable_the_orleans_sweep() => _options.DefunctSiloCleanupPeriod.ShouldBeNull();
}
