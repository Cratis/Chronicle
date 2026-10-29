// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.for_ClusterReleases;

public class when_the_manifest_changes : given.a_cluster_manifest
{
    IReadOnlyList<SiloAddress> _before;
    IReadOnlyList<SiloAddress> _after;
    void Establish() => _before = _releases.AtLeastAsNewAsThisSilo([_other]);
    void Because()
    {
        Set(_other, "19.113.0");
        _after = _releases.AtLeastAsNewAsThisSilo([_other]);
    }
    [Fact] void should_not_keep_a_stale_snapshot() => _after.ShouldContain(_other);
    [Fact] void should_have_excluded_the_preceding_release() => _before.ShouldBeEmpty();
}
