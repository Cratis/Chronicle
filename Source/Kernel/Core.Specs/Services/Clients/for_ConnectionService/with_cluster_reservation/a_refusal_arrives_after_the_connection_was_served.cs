// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

/// <summary>
/// A reservation that answers only after the grace does not hold the connection back; when its answer is a refusal the
/// stream ends and the claim is taken back without a trace, so that the connection's real owner is not locked out.
/// </summary>
public class a_refusal_arrives_after_the_connection_was_served : given.a_two_silo_cluster
{
    readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _served;
    Exception _ended;
    ConnectionClaim _ownersClaim;

    protected override TimeSpan ResponseTimeout => TimeSpan.FromSeconds(30);

    protected override IConnectionReservation Decorate(IConnectionReservation grain) => new Delaying(grain, _gate.Task);

    async Task Establish() => await Open(_serviceA, AuthenticatedCalls.As("alice"));

    async Task Because()
    {
        _served = await Open(_serviceB, AuthenticatedCalls.As("bob"));
        _gate.SetResult();
        _ended = await Terminated();
        _ownersClaim = _ownershipB.Claim(Id, AuthenticatedCalls.OwnerOf("alice"));
    }

    [Fact] void should_serve_the_client_while_the_reservation_is_silent() => _served.ShouldBeNull();
    [Fact] void should_end_the_stream_when_the_refusal_arrives() => ((RpcException)_ended).StatusCode.ShouldEqual(StatusCode.PermissionDenied);
    [Fact] void should_not_keep_a_claim_for_the_refused_caller() => Owns(_ownershipB, "bob").ShouldBeFalse();
    [Fact] void should_leave_the_real_owner_free_to_claim_the_connection() => _ownersClaim.ShouldEqual(ConnectionClaim.Claimed);

    sealed class Delaying(IConnectionReservation inner, Task gate) : IConnectionReservation
    {
        public async Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            if (owner.Equals(AuthenticatedCalls.OwnerOf("bob")))
            {
                await gate;
            }

            return await inner.Claim(owner, silo, token, lease);
        }

        public Task<bool> IsHeldBy(ConnectionOwner owner) => inner.IsHeldBy(owner);

        public Task Release(ConnectionOwner owner, Guid token) => inner.Release(owner, token);
    }
}
