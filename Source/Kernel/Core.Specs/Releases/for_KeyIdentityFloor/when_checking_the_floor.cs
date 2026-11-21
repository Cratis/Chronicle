// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases.for_KeyIdentityFloor;

public class when_checking_the_floor : Specification
{
    bool _unknown;
    bool _below;
    bool _at;
    bool _mixed;
    bool _unpublished;

    static IClusterReleases Releases(ClusterReleaseSummary summary)
    {
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(summary);
        return releases;
    }

    void Because()
    {
        _unknown = KeyIdentityFloor.IsReached(null);
        _below = KeyIdentityFloor.IsReached(Releases(new([(new SemanticVersion(19, 178, 9), 3)], 0, 0)));
        _at = KeyIdentityFloor.IsReached(Releases(new([(ClusterReleaseBoundary.KeyIdentity, 3)], 0, 0)));
        _mixed = KeyIdentityFloor.IsReached(Releases(new([(ClusterReleaseBoundary.KeyIdentity, 2), (new SemanticVersion(19, 178, 9), 1)], 0, 0)));
        _unpublished = KeyIdentityFloor.IsReached(Releases(new([(ClusterReleaseBoundary.KeyIdentity, 2)], 1, 0)));
    }

    [Fact] void should_not_be_reached_without_known_releases() => _unknown.ShouldBeFalse();
    [Fact] void should_not_be_reached_below_the_boundary() => _below.ShouldBeFalse();
    [Fact] void should_be_reached_at_the_boundary() => _at.ShouldBeTrue();
    [Fact] void should_not_be_reached_in_a_mixed_cluster() => _mixed.ShouldBeFalse();
    [Fact] void should_not_be_reached_while_a_silo_is_unpublished() => _unpublished.ShouldBeFalse();
}
