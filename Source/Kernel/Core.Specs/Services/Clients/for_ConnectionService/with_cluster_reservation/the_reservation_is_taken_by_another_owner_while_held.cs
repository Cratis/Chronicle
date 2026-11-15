// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Grpc.Core;

namespace Cratis.Chronicle.Services.Clients.for_ConnectionService.with_cluster_reservation;

public class the_reservation_is_taken_by_another_owner_while_held : given.a_two_silo_cluster
{
    Exception _result;

    protected override IConnectionReservation Decorate(IConnectionReservation grain) => new RefusingRenewals(grain);

    async Task Establish() => await Open(_serviceA, AuthenticatedCalls.As("alice"));

    async Task Because() => _result = await Terminated();

    [Fact] void should_close_the_connection_as_not_available() => ((RpcException)_result).StatusCode.ShouldEqual(StatusCode.PermissionDenied);

    sealed class RefusingRenewals(IConnectionReservation inner) : IConnectionReservation
    {
        readonly HashSet<Guid> _seen = [];

        public async Task<bool> Claim(ConnectionOwner owner, SiloAddress silo, Guid token, TimeSpan lease)
        {
            var granted = await inner.Claim(owner, silo, token, lease);
            return granted && _seen.Add(token);
        }

        public Task<bool> IsHeldBy(ConnectionOwner owner) => inner.IsHeldBy(owner);

        public Task Release(ConnectionOwner owner, Guid token) => inner.Release(owner, token);
    }
}
