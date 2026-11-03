// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Cluster.for_SqlResourceNameReservations;

public class when_keys_differ_only_in_case : given.reservations
{
    bool _first;
    bool _second;

    async Task Because()
    {
        _first = await Reservations.Reserve("A", ["ns:a"]);
        _second = await Reservations.Reserve("B", ["ns:A"]);
    }

    [Fact] void should_accept_the_first_key() => _first.ShouldBeTrue();
    [Fact] void should_not_fold_case_in_the_store() => _second.ShouldBeTrue();
}
