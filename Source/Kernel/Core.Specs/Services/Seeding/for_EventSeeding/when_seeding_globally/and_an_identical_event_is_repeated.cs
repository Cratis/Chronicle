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
/// The client buckets entries by event type before they reach the wire, so the interleaving the seeder
/// wrote is already gone by the time the service sees them - what the service owes is that every fact
/// survives and that it does not reorder what it was handed. Restoring the interleaving is a separate
/// question and is not what this specifies.
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
    [Fact] void should_keep_them_in_the_order_they_arrived() => EntriesSeededGlobally.Select(_ => _.EventTypeId.Value).ToArray().ShouldEqual<string[]>(["submitted", "submitted", "approved", "approved"]);
}
