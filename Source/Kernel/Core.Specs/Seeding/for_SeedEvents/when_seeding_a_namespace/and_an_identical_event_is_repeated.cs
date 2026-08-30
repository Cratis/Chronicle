// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Seeding.for_SeedEvents.when_seeding_a_namespace;

/// <summary>
/// The namespaced scope carries its own copy of the reconciliation, so it needs its own statement of the
/// same pair: a genuine repeat is two facts and both must land.
/// </summary>
public class and_an_identical_event_is_repeated : given.a_seeding_grain
{
    SeedEvents _request;

    void Establish() => _request = ANamespacedRequestFor(
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"));

    async Task Because() => await _request.Handle(_grainFactory);

    [Fact] void should_keep_both_facts() => EntriesSeededForTheNamespace.Count().ShouldEqual(2);
    [Fact] void should_not_reach_the_global_grain() => EntriesSeededGlobally.ShouldBeEmpty();
}
