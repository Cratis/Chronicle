// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_globally;

/// <summary>
/// Declared control - green on both sides of the multiplicity fix. Nothing in the contract obliges a client
/// to send both groupings, so an entry that arrives in only one of them still has to be seeded exactly once.
/// It is the lower bound the multiset reconciliation is built on: a count of one in either grouping means
/// one entry.
/// </summary>
public class and_an_entry_is_only_in_one_grouping : given.an_event_seeding_service
{
    SeedRequest _request;

    void Establish() => _request = new SeedRequest
    {
        EventStore = TheEventStore,
        GlobalByEventType =
        [
            new EventTypeSeedEntries
            {
                EventTypeId = "submitted",
                Entries = [AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}")]
            }
        ],
        GlobalByEventSource =
        [
            new EventSourceSeedEntries
            {
                EventSourceId = "timesheet-2",
                Entries = [AnEntry("timesheet-2", "approved", /*lang=json,strict*/ "{\"month\":6}")]
            }
        ]
    };

    async Task Because() => await _service.Seed(_request);

    [Fact] void should_seed_both_entries() => EntriesSeededGlobally.Count().ShouldEqual(2);
    [Fact] void should_seed_the_one_only_in_the_event_type_grouping() => EntriesSeededGlobally.Count(_ => _.EventSourceId.Value == "timesheet-1").ShouldEqual(1);
    [Fact] void should_seed_the_one_only_in_the_event_source_grouping() => EntriesSeededGlobally.Count(_ => _.EventSourceId.Value == "timesheet-2").ShouldEqual(1);
}
