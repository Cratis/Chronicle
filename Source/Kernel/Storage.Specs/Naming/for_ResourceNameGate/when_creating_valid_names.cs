// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_valid_names : given.a_gate
{
    IReadOnlyCollection<string> _storeKeys;
    string _storeOwner;
    Exception _store;
    Exception _namespace;
    Exception[] _valid;

    void Establish() =>
        _reservations.Reserve(Arg.Do<string>(_ => _storeOwner ??= _), Arg.Do<IReadOnlyCollection<string>>(_ => _storeKeys ??= _)).Returns(true);

    void Because()
    {
        _store = Catch.Exception(() => _gate.EnsureCanCreate(Store("Orders", ["other"])).GetAwaiter().GetResult());
        _namespace = Catch.Exception(() => _gate.EnsureCanCreate(Namespace("Orders", "Tenant")).GetAwaiter().GetResult());
        _valid = [.. ValidNames.Select(_ => Catch.Exception(() => _gate.EnsureCanCreate(Store(_)).GetAwaiter().GetResult()))];
    }

    [Fact] void should_create_the_store() => _store.ShouldBeNull();
    [Fact] void should_create_the_namespace() => _namespace.ShouldBeNull();
    [Fact] void should_create_valid_names() => _valid.ShouldEachConformTo(_ => _ is null);
    [Fact] void should_reserve_with_the_exact_owner() => _storeOwner.ShouldEqual("Orders");
    [Fact] void should_reserve_the_folded_store_key() => _storeKeys.ShouldContain("store:ORDERS");
}
