// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_SeedEvents.when_seeding_a_namespace;

/// <summary>
/// The other half of the pair for the namespaced scope: distinct entries sent twice, once per grouping,
/// still collapse to one each.
/// </summary>
public class and_the_same_entry_is_in_both_groupings : given.a_seeding_grain
{
    SeedEvents _request;

    void Establish() => _request = ANamespacedRequestFor(
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-1", "approved", /*lang=json,strict*/ "{\"month\":5}"));

    async Task Because() => await _request.Handle(_grainFactory);

    [Fact] void should_seed_each_entry_once() => EntriesSeededForTheNamespace.Count().ShouldEqual(2);
    [Fact] void should_seed_the_submission() => EntriesSeededForTheNamespace.Count(_ => _.EventTypeId.Value == "submitted").ShouldEqual(1);
    [Fact] void should_seed_the_approval() => EntriesSeededForTheNamespace.Count(_ => _.EventTypeId.Value == "approved").ShouldEqual(1);
}
