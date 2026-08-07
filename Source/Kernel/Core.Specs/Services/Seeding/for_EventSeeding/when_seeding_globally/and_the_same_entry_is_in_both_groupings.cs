// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_globally;

/// <summary>
/// The other half of the pair: the client sends every entry twice, once bucketed by event type and once by
/// event source, so the collapsing that keeps a genuine repeat must still fold the client's own double-send
/// back into one. Every entry here is distinct, and each arrives twice.
/// </summary>
public class and_the_same_entry_is_in_both_groupings : given.an_event_seeding_service
{
    SeedRequest _request;

    void Establish() => _request = AGlobalRequestFor(
        AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-1", "approved", /*lang=json,strict*/ "{\"month\":5}"),
        AnEntry("timesheet-2", "submitted", /*lang=json,strict*/ "{\"month\":6}"));

    async Task Because() => await _service.Seed(_request);

    [Fact] void should_seed_each_entry_once() => EntriesSeededGlobally.Count().ShouldEqual(3);
    [Fact] void should_have_sent_each_entry_twice_on_the_wire() => _request.GlobalByEventType.Sum(_ => _.Entries.Count).ShouldEqual(_request.GlobalByEventSource.Sum(_ => _.Entries.Count));
    [Fact] void should_keep_each_event_source_in_the_order_the_seeder_wrote() => EntriesSeededGlobally.Select(_ => $"{_.EventSourceId}/{_.EventTypeId}").ToArray().ShouldEqual<string[]>(["timesheet-1/submitted", "timesheet-1/approved", "timesheet-2/submitted"]);
}
