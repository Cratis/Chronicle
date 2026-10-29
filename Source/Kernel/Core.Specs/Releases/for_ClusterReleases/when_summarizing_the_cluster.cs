// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_ClusterReleases;

public class when_summarizing_the_cluster : given.a_cluster_manifest
{
    ClusterReleaseSummary _summary;
    void Because() => _summary = _releases.Summarize();
    [Fact] void should_order_releases() => _summary.Releases[0].Release.ToString().ShouldEqual("19.111.0");
    [Fact] void should_count_silos() => _summary.Releases[0].Silos.ShouldEqual(1);
    [Fact] void should_detect_mixed_releases() => _summary.IsMixed.ShouldBeTrue();
}
