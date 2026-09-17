// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compatibility.for_ProtocolVersionOrder.when_comparing;

public class two_versions : Specification
{
    [Fact] void should_report_the_lower_major_as_older() => ProtocolVersionOrder.Compare("1.0.0", "2.0.0").ShouldBeLessThan(0);

    [Fact] void should_report_the_higher_major_as_newer() => ProtocolVersionOrder.Compare("2.0.0", "1.0.0").ShouldBeGreaterThan(0);

    [Fact] void should_report_identical_versions_as_equal() => ProtocolVersionOrder.Compare("1.2.3", "1.2.3").ShouldEqual(0);

    [Fact]
    void should_compare_numerically_rather_than_lexicographically() =>

        // A string comparison would put "1.10.0" before "1.9.0" - protocol versions are numeric components,
        // not sortable strings.
        ProtocolVersionOrder.Compare("1.10.0", "1.9.0").ShouldBeGreaterThan(0);

    [Fact]
    void should_treat_a_missing_trailing_component_as_zero() =>
        ProtocolVersionOrder.Compare("1.0", "1.0.0").ShouldEqual(0);

    [Fact]
    void should_treat_an_empty_version_as_the_oldest_possible() =>

        // A client too old to send a protocol version at all must not be treated as newer than anything -
        // that would ask the newer side (the kernel) to serve the client, backwards from what an unversioned
        // caller can safely mean.
        ProtocolVersionOrder.Compare(string.Empty, "1.0.0").ShouldBeLessThan(0);

    [Fact]
    void should_treat_an_unparsable_component_as_zero() =>
        ProtocolVersionOrder.Compare("abc.def.ghi", "1.0.0").ShouldBeLessThan(0);
}
