// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_stream_ends_while_a_renewal_is_pending : given.a_two_silo_cluster
{
    readonly TaskCompletionSource _renewalStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _delayedAnswer = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _claims;
    int _claimsAtRelease;
    bool _delayedClaimGranted;

    protected override TimeSpan ResponseTimeout => TimeSpan.FromSeconds(30);

    protected override IConnectionReservation Decorate(IConnectionReservation grain) => new Delaying(grain, this);

    async Task Because()
    {
        using var cancellation = new CancellationTokenSource();
        await Open(_serviceA, AuthenticatedCalls.As("alice", cancellationToken: cancellation.Token));
        await _renewalStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await cancellation.CancelAsync();
        await Released();
        _claimsAtRelease = Volatile.Read(ref _claims);

        // The reservation answers the renewal only now, after the stream has ended and its claim was released.
        _delayedAnswer.TrySetResult(true);
        _delayedClaimGranted = await _delayedClaim.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    Task<bool> _delayedClaim = Task.FromResult(true);

    [Fact] void should_release_without_waiting_for_the_pending_renewal() => _claimsAtRelease.ShouldEqual(2);
    [Fact] void should_refuse_the_renewal_that_arrives_after_the_release() => _delayedClaimGranted.ShouldBeFalse();
    [Fact] void should_not_start_another_renewal_after_the_stream_ended() => Volatile.Read(ref _claims).ShouldEqual(2);

    sealed class Delaying(IConnectionReservation inner, the_stream_ends_while_a_renewal_is_pending spec) : IConnectionReservation
    {
        public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            if (Interlocked.Increment(ref spec._claims) == 1) return inner.Claim(owner, silo, token, lease);
            spec._delayedClaim = Late(owner, silo, token, lease);
            spec._renewalStarted.TrySetResult();
            return spec._delayedClaim;
        }

        public Task<bool> IsHeldBy(ConnectionOwner owner) => inner.IsHeldBy(owner);

        public Task Release(ConnectionOwner owner, Guid token) => inner.Release(owner, token);

        async Task<bool> Late(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            await spec._delayedAnswer.Task;
            return await inner.Claim(owner, silo, token, lease);
        }
    }
}
