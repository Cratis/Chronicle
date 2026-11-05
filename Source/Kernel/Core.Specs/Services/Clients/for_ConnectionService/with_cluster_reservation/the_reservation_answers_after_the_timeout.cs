// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_answers_after_the_timeout : given.a_two_silo_cluster
{
    Exception _result;
    bool _other;

    protected override IConnectionReservation Decorate(IConnectionReservation grain) => new AnsweringLate(grain);

    async Task Because()
    {
        _result = await Open(_serviceA, AuthenticatedCalls.As("alice"));
        await Released();
        _clock.Advance(ConnectionOwnership.ReleasedClaimRetention);
        _other = await _grain.Claim(AuthenticatedCalls.OwnerOf("bob"), _siloB, Guid.NewGuid(), TimeSpan.FromMinutes(1));
    }

    [Fact] void should_report_that_the_connection_could_not_be_verified() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.Unavailable);
    [Fact] void should_end_with_the_claim_released() => _other.ShouldBeTrue();

    sealed class AnsweringLate(IConnectionReservation inner) : IConnectionReservation
    {
        public async Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            // The grain processes the claim, but its answer never arrives in time.
            await inner.Claim(owner, silo, token, lease);
            return await new TaskCompletionSource<bool>().Task;
        }

        public Task Release(ConnectionOwner owner, Guid token) => inner.Release(owner, token);
    }
}
