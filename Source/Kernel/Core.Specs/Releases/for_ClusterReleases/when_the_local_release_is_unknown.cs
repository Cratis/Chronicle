// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Releases.for_ClusterReleases;

public class when_the_local_release_is_unknown : given.a_cluster_manifest
{
    IReadOnlyList<SiloAddress> _result;
    void Establish() => _releases = ForVersion(new(0, 0, 0));
    void Because() => _result = _releases.AtLeastAsNewAsThisSilo([_local, _other]);
    [Fact] void should_preserve_development_placement() => _result.ShouldContainOnly(_local, _other);
}
