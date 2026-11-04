// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameReservations;

public class when_reserving_keys : Specification
{
    FakeReservations _reservations;
    bool _first;
    bool _again;
    bool _other;
    bool _overlapping;
    Exception _failure;

    void Establish() => _reservations = new();

    void Because()
    {
        _first = _reservations.Reserve("Orders", ["a", "b"]).GetAwaiter().GetResult();
        _again = _reservations.Reserve("Orders", ["a", "b"]).GetAwaiter().GetResult();
        _other = _reservations.Reserve("orders", ["c", "b"]).GetAwaiter().GetResult();
        _reservations.FailOnKey = "e";
        _failure = Catch.Exception(() => _reservations.Reserve("Third", ["d", "e"]).GetAwaiter().GetResult());
        _reservations.FailOnKey = null;
        _overlapping = _reservations.Reserve("Orders", ["a", "x"]).GetAwaiter().GetResult();
    }

    [Fact] void should_reserve_the_keys() => _first.ShouldBeTrue();
    [Fact] void should_not_conflict_with_keys_held_by_the_same_owner() => _again.ShouldBeTrue();
    [Fact] void should_refuse_an_owner_differing_only_in_case() => _other.ShouldBeFalse();
    [Fact] void should_roll_back_the_keys_of_the_refused_owner() => _reservations.Held.ContainsKey("c").ShouldBeFalse();
    [Fact] void should_keep_the_keys_of_the_first_owner() => _reservations.Held["b"].ShouldEqual("Orders");
    [Fact] void should_roll_back_and_rethrow_when_the_store_fails() => _failure.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_not_keep_keys_of_a_failed_reservation() => _reservations.Held.ContainsKey("d").ShouldBeFalse();
    [Fact] void should_reserve_the_new_keys_of_an_owner_holding_some() => _overlapping.ShouldBeTrue();
}
