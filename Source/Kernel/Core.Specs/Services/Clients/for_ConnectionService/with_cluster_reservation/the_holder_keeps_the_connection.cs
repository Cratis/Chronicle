// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_holder_keeps_the_connection : given.a_two_silo_cluster
{
    readonly TaskCompletionSource _renewed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    Exception _result;

    protected override IConnectionReservation Decorate(IConnectionReservation grain) => new Watching(grain, _renewed);

    async Task Because()
    {
        _result = await Open(_serviceA, AuthenticatedCalls.As("alice"));
        await _renewed.Task.WaitAsync(TimeSpan.FromSeconds(10), TimeProvider.System);
    }

    [Fact] void should_be_served() => _result.ShouldBeNull();
    [Fact] void should_have_renewed_the_reservation_with_the_same_stream() => _renewed.Task.IsCompletedSuccessfully.ShouldBeTrue();

    sealed class Watching(IConnectionReservation inner, TaskCompletionSource renewed) : IConnectionReservation
    {
        readonly HashSet<Guid> _seen = [];

        public Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            if (!_seen.Add(token)) renewed.TrySetResult();
            return inner.Claim(owner, silo, token, lease);
        }

        public Task Release(ConnectionOwner owner, Guid token) => inner.Release(owner, token);
    }
}
