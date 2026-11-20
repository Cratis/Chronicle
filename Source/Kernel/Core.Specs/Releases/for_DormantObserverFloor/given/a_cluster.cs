// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases.for_DormantObserverFloor.given;

public class a_cluster : Specification
{
    protected IClusterReleases _releases;

    protected void Cluster(int unpublished, int omitted, params (SemanticVersion Release, int Silos)[] releases)
    {
        _releases = Substitute.For<IClusterReleases>();
        _releases.Summarize().Returns(new ClusterReleaseSummary(releases, unpublished, omitted));
    }
}
