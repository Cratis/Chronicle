// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.System;

namespace Cratis.Chronicle.Releases.for_TypedProtectedValuesFloor;

public class when_checking_the_floor : Specification
{
    static bool Reached(ClusterReleaseSummary summary)
    {
        var releases = Substitute.For<IClusterReleases>();
        releases.Summarize().Returns(summary);
        return TypedProtectedValuesFloor.IsReached(releases);
    }

    [Fact] void should_not_be_reached_without_releases() => TypedProtectedValuesFloor.IsReached(null).ShouldBeFalse();
    [Fact] void should_not_be_reached_below_key_identity() => Reached(new([(new SemanticVersion(19, 178, 0), 1)], 0, 0)).ShouldBeFalse();
    [Fact] void should_not_be_reached_at_key_identity() => Reached(new([(ClusterReleaseBoundary.KeyIdentity, 1)], 0, 0)).ShouldBeFalse();
    [Fact] void should_not_be_reached_before_the_typed_boundary() => Reached(new([(new SemanticVersion(19, 195, 0), 1)], 0, 0)).ShouldBeFalse();
    [Fact] void should_be_reached_at_the_typed_boundary() => Reached(new([(ClusterReleaseBoundary.TypedProtectedValues, 2)], 0, 0)).ShouldBeTrue();
    [Fact] void should_not_be_reached_in_a_mixed_cluster() => Reached(new([(ClusterReleaseBoundary.TypedProtectedValues, 1), (new SemanticVersion(19, 195, 0), 1)], 0, 0)).ShouldBeFalse();
    [Fact] void should_not_be_reached_with_unpublished_silos() => Reached(new([(ClusterReleaseBoundary.TypedProtectedValues, 1)], 1, 0)).ShouldBeFalse();
    [Fact] void should_not_be_reached_with_omitted_silos() => Reached(new([(ClusterReleaseBoundary.TypedProtectedValues, 1)], 0, 1)).ShouldBeFalse();
}
