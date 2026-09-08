// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using SeededEntry = Cratis.Chronicle.Seeding.SeedingEntry;

namespace Cratis.Chronicle.Seeding.for_SeedEvents.when_seeding_a_namespace;

/// <summary>
/// Namespace-specific seeding has the same external completion contract as global seeding: a rejected entry
/// must make the operation fail so the caller can retry the idempotent batch.
/// </summary>
public class and_the_grain_reports_incomplete_seeding : given.a_seeding_grain
{
    SeedEvents _request;
    Exception _error;

    void Establish()
    {
        _request = ANamespacedRequestFor(AnEntry("timesheet-1", "submitted", /*lang=json,strict*/ "{\"month\":5}"));
        _namespaceGrain.SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>()).Returns(Task.FromResult(SeedingResult.Incomplete));
    }

    async Task Because() => _error = await Catch.Exception(() => _request.Handle(_grainFactory));

    /// <summary>
    /// Must NOT fail the operation. The client seeds inside <c>RegisterAll</c>, which runs from
    /// <c>OnConnected</c>, and a failing handler there rolls the connection back to disconnected - so a
    /// deterministic seed rejection would reconnect, be rejected again, and hold the client offline for good.
    /// The grain logs the rejection and leaves the batch unseeded for a corrected run.
    /// </summary>
    [Fact] void should_not_fail_the_external_operation() => _error.ShouldBeNull();
    [Fact] void should_offer_the_entries_once() => _namespaceGrain.Received(1).SeedWithResult(Arg.Any<IEnumerable<SeededEntry>>());
}
