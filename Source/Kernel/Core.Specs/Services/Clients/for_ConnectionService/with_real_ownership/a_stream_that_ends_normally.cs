// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_real_ownership;

public class a_stream_that_ends_normally : given.a_service_with_real_ownership
{
    readonly CancellationTokenSource _cancellation = new();
    bool _ownedAfterTheStreamEnded;
    ConnectionClaim _claimedAgain;

    async Task Establish()
    {
        await Open(AuthenticatedCalls.As("alice", cancellationToken: _cancellation.Token));
        Owns("alice").ShouldBeTrue();
    }

    async Task Because()
    {
        await _cancellation.CancelAsync();
        await ClaimReleased();
        _ownedAfterTheStreamEnded = Owns("alice");
        _claimedAgain = _ownership.Claim(Id, AuthenticatedCalls.OwnerOf("alice"));
    }

    [Fact] void should_release_the_claim() => _ownedAfterTheStreamEnded.ShouldBeFalse();
    [Fact] void should_let_the_same_owner_claim_it_again() => _claimedAgain.ShouldEqual(ConnectionClaim.Claimed);
}
