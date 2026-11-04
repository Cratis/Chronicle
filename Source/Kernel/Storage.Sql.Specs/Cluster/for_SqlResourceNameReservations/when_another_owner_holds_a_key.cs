// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceNameReservations;

public class when_another_owner_holds_a_key : given.reservations
{
    bool _first;
    bool _second;
    List<ResourceNameReservationEntity> _rows;

    async Task Because()
    {
        _first = await Reservations.Reserve("A", ["store:ORDERS"]);
        _second = await Reservations.Reserve("B", ["store:ORDERS", "res:X"]);
        _rows = await Rows();
    }

    [Fact] void should_reserve_for_the_first_owner() => _first.ShouldBeTrue();
    [Fact] void should_refuse_the_second_owner() => _second.ShouldBeFalse();
    [Fact] void should_keep_only_the_first_owners_row() => _rows.Select(row => (row.Key, row.Owner)).ShouldEqual([("store:ORDERS", "A")]);
}
