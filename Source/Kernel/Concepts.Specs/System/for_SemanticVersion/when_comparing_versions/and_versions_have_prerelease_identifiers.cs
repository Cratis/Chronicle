// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.System.for_SemanticVersion.when_comparing_versions;

public class and_versions_have_prerelease_identifiers : Specification
{
    [Theory]
    [InlineData("2.0.0-rc.2", "2.0.0-rc.10")]
    [InlineData("2.0.0-alpha", "2.0.0-alpha.1")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    [InlineData("2.0.0-2", "2.0.0-alpha")]
    [InlineData("2.0.0-alpha", "2.0.0-beta")]
    [InlineData("2.0.0-999999999999999999999999999999", "2.0.0-1000000000000000000000000000000")]
    void should_order_versions_in_both_directions(string lower, string higher)
    {
        var older = SemanticVersion.Parse(lower);
        var newer = SemanticVersion.Parse(higher);
        older.CompareTo(newer).ShouldBeLessThan(0);
        newer.CompareTo(older).ShouldBeGreaterThan(0);
        (older < newer).ShouldBeTrue();
        (older <= newer).ShouldBeTrue();
        (newer > older).ShouldBeTrue();
        (newer >= older).ShouldBeTrue();
    }

    [Theory]
    [InlineData("2.0.0-rc.2+build.1", "2.0.0-rc.2+build.10")]
    [InlineData("2.0.0+build.10", "2.0.0+build.2")]
    void should_ignore_build_metadata(string left, string right)
    {
        var first = SemanticVersion.Parse(left);
        var second = SemanticVersion.Parse(right);
        first.CompareTo(second).ShouldEqual(0);
        second.CompareTo(first).ShouldEqual(0);
        (first < second).ShouldBeFalse();
        (first > second).ShouldBeFalse();
    }
}
