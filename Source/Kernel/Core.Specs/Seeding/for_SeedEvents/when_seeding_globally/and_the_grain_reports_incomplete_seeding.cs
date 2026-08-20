// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SeededEntry = Cratis.Chronicle.Seeding.SeedingEntry;

namespace Cratis.Chronicle.Seeding.for_SeedEvents.when_seeding_globally;

/// <summary>
/// An incomplete result means at least one event was rejected and remains unseeded. The external operation
/// must fail so its caller retains the entries and can offer them again instead of treating partial state as
/// completed bootstrap data.
/// </summary>
public class and_the_grain_reports_incomplete_seeding : given.a_seeding_grain
{
    SeedEvents _request;
    Exception _error;

    void Establish()
    {
        _request = AGlobalRequestFor(AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"));
        _globalGrain.SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
    }

    async Task Because() => _error = await Catch.Exception(() => _request.Handle(_grainFactory));

    /// <summary>
    /// Must NOT fail the operation. The client seeds inside <c>RegisterAll</c>, which runs from
    /// <c>OnConnected</c>, and a failing handler there rolls the connection back to disconnected - so a
    /// deterministic seed rejection would reconnect, be rejected again, and hold the client offline for good.
    /// The grain logs the rejection and leaves the batch unseeded for a corrected run.
    /// </summary>
    [Fact] void should_not_fail_the_external_operation() => _error.ShouldBeNull();
    [Fact] void should_offer_the_entries_once() => _globalGrain.Received(1).SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>());
}
