// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_DatabaseLocation.when_deciding;

public class and_no_database_was_seen : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    void should_require_positive_visibility_for_absence(bool? visibility) =>
        (DatabaseLocation.Decide(null, null, visibility, true, null) is DatabaseLocation.Absent).ShouldEqual(visibility == true);

    [Fact]
    void should_fail_closed_for_an_unsupported_provider() =>
        (DatabaseLocation.Decide(null, null, true, false, null) is DatabaseLocation.Unknown).ShouldBeTrue();

    [Fact]
    void should_not_turn_a_probe_failure_into_absence() =>
        DatabaseLocation.Decide(null, null, true, true, new InvalidOperationException("not included"), "visibility")
            .ShouldEqual(new DatabaseLocation.Unknown("visibility", nameof(InvalidOperationException)));
}
