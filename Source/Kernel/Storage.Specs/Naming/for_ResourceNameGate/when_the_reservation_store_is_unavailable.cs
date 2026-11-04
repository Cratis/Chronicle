// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_the_reservation_store_is_unavailable : given.a_gate
{
    Exception _new;
    Exception _registered;
    Exception _opened;

    void Establish()
    {
        _snapshot.Publish([], []);
        _reservations.Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>()).Returns<bool>(_ => throw new InvalidOperationException("The reservation store is down."));
    }

    async Task Because()
    {
        _new = await Catch.Exception(() => _gate.EnsureCanCreate(Store("fresh")));
        _reservations.ClearReceivedCalls();
        _registered = await Catch.Exception(() => _gate.EnsureCanCreate(Store("orders", ["orders"])));
        _opened = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("orders")));
    }

    [Fact] void should_refuse_a_new_name_with_the_error_of_the_outage() => _new.ShouldBeOfExactType<InvalidOperationException>();
    [Fact] void should_still_accept_a_registered_owner() => _registered.ShouldBeNull();
    [Fact] void should_still_open_a_name() => _opened.ShouldBeNull();
    [Fact] void should_not_read_a_reservation_for_a_registered_owner_or_when_opening() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
