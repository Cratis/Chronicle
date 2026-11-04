// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_reserving_a_name_longer_than_the_key_limit : given.a_gate
{
    string[] _lower;
    string[] _upper;
    string[] _again;
    string[] _different;
    string[] _short;

    void Because()
    {
        _lower = KeysFor(new string('a', 600));
        _upper = KeysFor(new string('A', 600));
        _again = KeysFor(new string('a', 600));
        _different = KeysFor(new string('a', 599) + "b");
        _short = KeysFor("orders");
    }

    [Fact] void should_keep_the_key_within_the_limit() => _lower.ShouldEachConformTo(_ => _.Length <= ResourceNameGate.MaximumStoredLength);
    [Fact] void should_keep_the_kind() => _lower[0].StartsWith("store:h:", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_give_the_same_key_for_the_same_name() => _lower.ShouldEqual(_again);
    [Fact] void should_give_the_same_key_for_a_case_variant() => _lower.ShouldEqual(_upper);
    [Fact] void should_give_a_different_key_for_a_different_name() => _different[0].ShouldNotEqual(_lower[0]);
    [Fact] void should_leave_short_keys_as_they_are() => _short[0].ShouldEqual("store:ORDERS");

    string[] KeysFor(string name)
    {
        _reservations.ClearReceivedCalls();
        _gate.EnsureCanCreate(Store(name)).GetAwaiter().GetResult();
        var call = _reservations.ReceivedCalls().Single(_ => _.GetMethodInfo().Name == nameof(IResourceNameReservations.Reserve));
        return ((IReadOnlyCollection<string>)call.GetArguments()[1]!).ToArray();
    }
}
