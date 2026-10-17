// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.for_ProjectionVersions;

public class when_checking_whether_a_version_is_older : Specification
{
    [Theory]
    [InlineData("1.0.0", "2.0.0")]
    [InlineData("1.9.0", "1.10.0")]
    [InlineData("1.2.3", "1.2.4")]
    [InlineData("2.0.0-rc.1", "2.0.0")]
    [InlineData("2.0.0-rc.2", "2.0.0-rc.10")]
    [InlineData("2.0.0-alpha", "2.0.0-alpha.1")]
    [InlineData("1.0.0-rc.1", "1.0.0")]
    [InlineData("2.0.0-2", "2.0.0-alpha")]
    [InlineData("2.0.0+build.1", "2.0.1+build.0")]
    void should_be_older(string candidate, string reference) => ProjectionVersions.IsOlder(candidate, reference).ShouldBeTrue();

    [Theory]
    [InlineData("2.0.0", "1.0.0")]
    [InlineData("1.2.3", "1.2.3")]
    [InlineData("1.2.3+build.1", "1.2.3+build.2")]
    [InlineData("2.0.0", "2.0.0-rc.1")]
    [InlineData("2.0.0-rc.10", "2.0.0-rc.2")]
    [InlineData("2.0.0-alpha.1", "2.0.0-alpha")]
    [InlineData("1.0.0", "1.0.0-rc.1")]
    [InlineData("2.0.0-alpha", "2.0.0-2")]
    void should_not_be_older(string candidate, string reference) => ProjectionVersions.IsOlder(candidate, reference).ShouldBeFalse();

    [Theory]
    [InlineData(null, "1.0.0")]
    [InlineData("1.0.0", null)]
    [InlineData("", "1.0.0")]
    [InlineData("1.0.0", " ")]
    [InlineData("not-a-version", "1.0.0")]
    [InlineData("1.0.0", "not-a-version")]
    [InlineData("1.0", "2.0.0")]
    void should_not_be_older_when_either_is_missing_or_unparseable(string? candidate, string? reference) => ProjectionVersions.IsOlder(candidate, reference).ShouldBeFalse();
}
