// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_hangs_during_renewal : given.a_two_silo_cluster
{
    readonly TaskCompletionSource _renewalStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly TaskCompletionSource<bool> _never = new(TaskCreationOptions.RunContinuationsAsynchronously);
    int _claims;
    int _keepAlivesWhenRenewalStarted;
    int _keepAlivesWhileRenewalHangs;
    int _claimsWhileRenewalHangs;

    protected override TimeSpan ResponseTimeout => TimeSpan.FromSeconds(30);

    protected override IConnectionReservation Decorate(IConnectionReservation grain) => new Hanging(grain, this);

    async Task Because()
    {
        await Open(_serviceA, AuthenticatedCalls.As("alice"));
        await _renewalStarted.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
        await KeepAlivesReached(_keepAlivesWhenRenewalStarted + 1);
        _keepAlivesWhileRenewalHangs = KeepAlivesReceived;
        _claimsWhileRenewalHangs = Volatile.Read(ref _claims);
    }

    [Fact] void should_keep_sending_keep_alives_while_the_renewal_is_waiting() => _keepAlivesWhileRenewalHangs.ShouldBeGreaterThanOrEqual(_keepAlivesWhenRenewalStarted + 1);
    [Fact] void should_not_start_a_second_renewal_while_one_is_waiting() => _claimsWhileRenewalHangs.ShouldEqual(2);
    [Fact] void should_not_have_the_renewal_answered() => _never.Task.IsCompleted.ShouldBeFalse();

    sealed class Hanging(IConnectionReservation inner, the_reservation_hangs_during_renewal spec) : IConnectionReservation
    {
        public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            if (Interlocked.Increment(ref spec._claims) == 1) return inner.Claim(owner, silo, token, lease);
            spec._keepAlivesWhenRenewalStarted = spec.KeepAlivesReceived;
            spec._renewalStarted.TrySetResult();
            return spec._never.Task;
        }

        public Task Release(ConnectionOwner owner, Guid token) => inner.Release(owner, token);
    }
}
