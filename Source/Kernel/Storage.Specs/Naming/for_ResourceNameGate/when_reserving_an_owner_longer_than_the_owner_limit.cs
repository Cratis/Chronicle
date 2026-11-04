// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_reserving_an_owner_longer_than_the_owner_limit : given.a_gate
{
    string _long;
    string _longAgain;
    string _short;

    void Because()
    {
        _long = OwnerFor(new string('a', 600));
        _longAgain = OwnerFor(new string('a', 600));
        _short = OwnerFor("orders");
    }

    [Fact] void should_keep_the_owner_within_the_limit() => _long.Length.ShouldBeLessThanOrEqual(ResourceNameGate.MaximumStoredLength);
    [Fact] void should_store_the_owner_as_a_hash() => _long.StartsWith("h:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_give_the_same_owner_for_the_same_name() => _long.ShouldEqual(_longAgain);
    [Fact] void should_leave_a_short_owner_as_it_is() => _short.ShouldEqual("orders");

    string OwnerFor(string name)
    {
        _reservations.ClearReceivedCalls();
        _gate.EnsureCanCreate(Store(name)).GetAwaiter().GetResult();
        var call = _reservations.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(IResourceNameReservations.Reserve));
        return (string)call.GetArguments()[0]!;
    }
}
