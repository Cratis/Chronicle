// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.for_ClusterReleases;

public class when_many_releases_are_present : given.a_cluster_manifest
{
    ClusterReleaseSummary _summary;
    void Establish()
    {
        for (var index = 0; index < 10; index++) Add($"20.{index}.0");
    }
    void Because() => _summary = _releases.Summarize();
    [Fact] void should_bound_the_release_list() => _summary.Releases.Count.ShouldEqual(8);
    [Fact] void should_count_omitted_releases() => _summary.Omitted.ShouldEqual(4);
}
