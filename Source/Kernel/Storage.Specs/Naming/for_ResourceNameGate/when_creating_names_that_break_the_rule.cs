// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_creating_names_that_break_the_rule : given.a_gate
{
    Exception[] _eventStores;
    Exception[] _namespaces;
    Exception _combination;
    Exception _registeredLegacy;

    void Establish() => _gate.MarkRegistered(new EventStoreName("legacy+store"));

    void Because()
    {
        _eventStores = [.. HostileEventStores.Select(_ => Catch.Exception(() => _gate.EnsureCanCreate(Store(_)).GetAwaiter().GetResult()))];
        _namespaces = [.. HostileNamespaces.Select(_ => Catch.Exception(() => _gate.EnsureCanCreate(Namespace("orders", _)).GetAwaiter().GetResult()))];
        _combination = Catch.Exception(() => _gate.EnsureCanCreate(Namespace("Chronicle", "MAIN")).GetAwaiter().GetResult());
        _registeredLegacy = Catch.Exception(() => _gate.EnsureCanCreate(Store("legacy+store")).GetAwaiter().GetResult());
    }

    [Fact] void should_refuse_hostile_event_stores() => _eventStores.ShouldEachConformTo(_ => _ is InvalidResourceName);
    [Fact] void should_refuse_hostile_namespaces() => _namespaces.ShouldEachConformTo(_ => _ is InvalidResourceName);
    [Fact] void should_refuse_the_cluster_combination() => _combination.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_not_grandfather_a_new_name() => _registeredLegacy.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_not_reserve_anything() => _reservations.DidNotReceive().Reserve(Arg.Any<string>(), Arg.Any<IReadOnlyCollection<string>>());
}
