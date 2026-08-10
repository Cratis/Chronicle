// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Seeding;
using Cratis.Chronicle.Seeding;

using SeededEntry = Cratis.Chronicle.Seeding.SeedingEntry;

namespace Cratis.Chronicle.Services.Seeding.for_EventSeeding.when_seeding_a_namespace;

/// <summary>
/// Namespace-specific seeding has the same external completion contract as global seeding: a rejected entry
/// must make the operation fail so the caller can retry the idempotent batch.
/// </summary>
public class and_the_grain_reports_incomplete_seeding : given.an_event_seeding_service
{
    SeedRequest _request;
    Exception _error;

    void Establish()
    {
        _request = ANamespacedRequestFor(AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"));
        _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
    }

    async Task Because() => _error = await Catch.Exception(() => _service.Seed(_request));

    [Fact] void should_fail_the_external_operation() => _error.ShouldBeOfExactType<EventSeedingIncomplete>();
    [Fact] void should_offer_the_entries_once() => _namespaceGrain.Received(1).SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>());
}
