// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceNameReservations;

public class when_the_same_owner_reserves_again : given.reservations
{
    bool _first;
    bool _second;
    List<ResourceNameReservationEntity> _rows;

    async Task Because()
    {
        _first = await Reservations.Reserve("A", ["store:ORDERS"]);
        _second = await Reservations.Reserve("A", ["store:ORDERS"]);
        _rows = await Rows();
    }

    [Fact] void should_accept_the_first_reservation() => _first.ShouldBeTrue();
    [Fact] void should_accept_the_repeat() => _second.ShouldBeTrue();
    [Fact] void should_hold_one_row() => _rows.Count.ShouldEqual(1);
}
