// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_globally;

/// <summary>
/// Tags are part of what an entry is - the seeding grain compares them when it decides whether an entry has
/// been seeded already. Two entries that agree on event source, event type and content but carry different
/// tags are two different entries, and the collapsing must not fold them into one.
/// </summary>
public class and_two_entries_differ_only_by_tags : given.an_event_seeding_service
{
    SeedRequest _request;

    void Establish() => _request = AGlobalRequestFor(
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}", "internal"),
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}", "external"));

    async Task Because() => await _service.Seed(_request);

    [Fact] void should_keep_both_entries() => EntriesSeededGlobally.Count().ShouldEqual(2);
    [Fact] void should_keep_the_internal_one() => EntriesSeededGlobally.Any(_ => _.Tags.Any(t => t.Value == "internal")).ShouldBeTrue();
    [Fact] void should_keep_the_external_one() => EntriesSeededGlobally.Any(_ => _.Tags.Any(t => t.Value == "external")).ShouldBeTrue();
}
