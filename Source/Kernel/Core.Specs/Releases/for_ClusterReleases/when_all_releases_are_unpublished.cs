// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_ClusterReleases;

public class when_all_releases_are_unpublished : given.a_cluster_manifest
{
    ClusterReleaseSummary _summary;
    void Establish()
    {
        Set(_local, null);
        Set(_other, null);
    }
    void Because() => _summary = _releases.Summarize();
    [Fact] void should_not_call_a_homogeneous_cluster_mixed() => _summary.IsMixed.ShouldBeFalse();
    [Fact] void should_count_both_silos() => _summary.Unpublished.ShouldEqual(2);
}
