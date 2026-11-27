// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_SinkProvenanceCache;

public class when_the_second_route_is_reseeded : given.two_routes
{
    void Establish() => _secondState = _secondState with { DatabaseId = new(Guid.NewGuid()) };

    void Because() => _clock.Advance(TimeSpan.FromSeconds(1));

    [Fact] void should_deny_the_existing_lease() => Catch.Exception(_lease.Check).ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_deny_the_second_route() => Catch.Exception(_secondLease.Check).ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_revoke_the_lease() => _lease.Revoked.IsCancellationRequested.ShouldBeFalse();

    [Fact]
    async Task should_not_recover_from_clean_first_route_completion_alone() =>
        (await Catch.Exception(async () =>
        {
            await _cached.CompleteInventory(false);
            await _lease.Ensure();
        })).ShouldBeOfExactType<InvalidOperationException>();
}
