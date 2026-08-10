// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Seeding;

using SeededEntry = Cratis.Chronicle.Seeding.SeedingEntry;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_globally;

/// <summary>
/// An incomplete result means at least one event was rejected and remains unseeded. The external operation
/// must fail so its caller retains the entries and can offer them again instead of treating partial state as
/// completed bootstrap data.
/// </summary>
public class and_the_grain_reports_incomplete_seeding : given.an_event_seeding_service
{
    SeedRequest _request;
    Exception _error;

    void Establish()
    {
        _request = AGlobalRequestFor(AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"));
        _globalGrain.SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
    }

    async Task Because() => _error = await Catch.Exception(() => _service.Seed(_request));

    [Fact] void should_fail_the_external_operation() => _error.ShouldBeOfExactType<EventSeedingIncomplete>();
    [Fact] void should_offer_the_entries_once() => _globalGrain.Received(1).SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>());
}
