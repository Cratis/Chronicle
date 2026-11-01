// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_DatabaseLocation.when_deciding;

public class and_a_database_was_found : Specification
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    void should_use_a_binding_without_requiring_visibility(bool? visibility) =>
        DatabaseLocation.Decide("bound", "legacy", visibility, true, null).ShouldEqual(new DatabaseLocation.Found("bound"));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [InlineData(null)]
    void should_use_a_seen_legacy_database_without_requiring_visibility(bool? visibility) =>
        DatabaseLocation.Decide(null, "legacy", visibility, true, null).ShouldEqual(new DatabaseLocation.Found("legacy"));

    [Fact]
    void should_not_hide_a_binding_lookup_failure() =>
        DatabaseLocation.Decide(null, null, null, true, new InvalidOperationException("not included"), "binding")
            .ShouldEqual(new DatabaseLocation.Unknown("binding", nameof(InvalidOperationException)));
}
