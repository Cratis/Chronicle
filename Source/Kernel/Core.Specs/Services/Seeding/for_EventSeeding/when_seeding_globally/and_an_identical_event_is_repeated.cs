// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_globally;

/// <summary>
/// Two events of the same type, on the same event source, carrying the same payload are not duplicates - in
/// an event-sourced system they are two facts that really happened. A timesheet submitted, approved,
/// corrected, submitted again and approved again yields two byte-identical submissions, and collapsing them
/// on value turns that history into one the domain's own state machine cannot produce.
/// </summary>
/// <remarks>
/// The order is half of the claim. The client sends the entries bucketed two ways, and only the
/// by-event-source bucketing carries the sequence the seeder wrote - so the history the event source ends
/// up with is submitted, approved, submitted, approved, and not the type-by-type interleaving.
/// </remarks>
public class and_an_identical_event_is_repeated : given.an_event_seeding_service
{
    SeedRequest _request;

    void Establish() => _request = AGlobalRequestFor(
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-1", "approved", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-1", "approved", /*lang=json,strict*/ "{\"month\":5}"));

    async Task Because() => await _service.Seed(_request);

    [Fact] void should_keep_every_fact() => EntriesSeededGlobally.Count().ShouldEqual(4);
    [Fact] void should_keep_both_submissions() => EntriesSeededGlobally.Count(_ => _.EventTypeId.Value == "submitted").ShouldEqual(2);
    [Fact] void should_keep_both_approvals() => EntriesSeededGlobally.Count(_ => _.EventTypeId.Value == "approved").ShouldEqual(2);
    [Fact] void should_keep_the_history_the_seeder_wrote() => EntriesSeededGlobally.Select(_ => _.EventTypeId.Value).ToArray().ShouldEqual<string[]>(["submitted", "approved", "submitted", "approved"]);
}
