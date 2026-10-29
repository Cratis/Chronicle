// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.for_ClusterReleases;

public class when_the_manifest_contains_hostile_values : given.a_cluster_manifest
{
    ClusterReleaseSummary _summary;
    void Establish()
    {
        Set(_other, new string('x', 10000) + "\n{Placeholder}");
        Add("19.111.0\n");
        Add("19.111.0-{Placeholder}");
    }
    void Because() => _summary = _releases.Summarize();
    [Fact] void should_count_invalid_values_as_unpublished() => _summary.Unpublished.ShouldEqual(3);
    [Fact] void should_not_echo_manifest_text() => _summary.ToString().ShouldNotContain("Placeholder");
    [Fact] void should_bound_the_output() => _summary.ToString().Length.ShouldBeLessThan(600);
    [Fact] void should_detect_published_and_unpublished_silos_as_mixed() => _summary.IsMixed.ShouldBeTrue();
}
