// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_ClusterReleases;

public class when_releases_are_homogeneous : given.a_cluster_manifest
{
    ClusterReleaseSummary _summary;
    IReadOnlyList<SiloAddress> _result;
    void Establish() => Set(_other, "19.112.0");
    void Because()
    {
        _summary = _releases.Summarize();
        _result = _releases.AtLeastAsNewAsThisSilo([_local, _other]);
    }
    [Fact] void should_keep_both_candidates() => _result.ShouldContainOnly(_local, _other);
    [Fact] void should_not_report_mixed_releases() => _summary.IsMixed.ShouldBeFalse();
    [Fact] void should_group_silos_of_the_same_release() => _summary.Releases.Single().Silos.ShouldEqual(2);
}
