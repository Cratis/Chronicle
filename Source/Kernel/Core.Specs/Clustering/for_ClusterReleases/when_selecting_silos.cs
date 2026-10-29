// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Clustering.for_ClusterReleases;

public class when_selecting_silos : given.a_cluster_manifest
{
    SiloAddress _newer;
    SiloAddress _prerelease;
    SiloAddress _build;
    SiloAddress _unpublished;
    SiloAddress _invalid;
    SiloAddress _noObserver;
    IReadOnlyList<SiloAddress> _result;

    void Establish()
    {
        _newer = Add("19.113.0");
        _prerelease = Add("19.112.0-rc.1");
        _build = Add("19.112.0+build.1");
        _unpublished = Add(null);
        _invalid = Add("not-a-release");
        _noObserver = Add("19.113.0");
        RemoveObserver(_noObserver);
    }

    void Because() => _result = _releases.AtLeastAsNewAsThisSilo(_silos.Keys);

    [Fact] void should_keep_this_release() => _result.ShouldContain(_local);
    [Fact] void should_exclude_older_releases() => _result.ShouldNotContain(_other);
    [Fact] void should_keep_newer_releases() => _result.ShouldContain(_newer);
    [Fact] void should_exclude_prereleases_of_this_release() => _result.ShouldNotContain(_prerelease);
    [Fact] void should_ignore_build_metadata_for_ordering() => _result.ShouldContain(_build);
    [Fact] void should_exclude_unpublished_releases() => _result.ShouldNotContain(_unpublished);
    [Fact] void should_exclude_invalid_releases() => _result.ShouldNotContain(_invalid);
    [Fact] void should_exclude_silos_without_the_observer_interface() => _result.ShouldNotContain(_noObserver);
}
