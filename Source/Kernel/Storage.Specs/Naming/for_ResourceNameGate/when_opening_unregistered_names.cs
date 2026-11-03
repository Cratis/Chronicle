// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_opening_unregistered_names : given.a_gate
{
    Exception[] _eventStores;
    Exception[] _namespaces;
    Exception[] _valid;
    Exception _combination;
    Exception _mixedCombination;

    void Because()
    {
        _eventStores = [.. HostileEventStores.Select(_ => Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName(_))))];
        _namespaces = [.. HostileNamespaces.Select(_ => Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("orders"), new EventStoreNamespaceName(_))))];
        _valid = [.. ValidNames.Select(_ => Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName(_), new EventStoreNamespaceName(_ == "Default" ? "main" : _))))];
        _combination = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("chronicle"), new EventStoreNamespaceName("main")));
        _mixedCombination = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("Chronicle"), new EventStoreNamespaceName("MAIN")));
    }

    [Fact] void should_refuse_hostile_event_stores() => _eventStores.ShouldEachConformTo(_ => _ is InvalidResourceName);
    [Fact] void should_refuse_hostile_namespaces() => _namespaces.ShouldEachConformTo(_ => _ is InvalidResourceName);
    [Fact] void should_accept_valid_names() => _valid.ShouldEachConformTo(_ => _ is null);
    [Fact] void should_refuse_the_cluster_combination() => _combination.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_refuse_the_cluster_combination_in_any_case() => _mixedCombination.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_use_the_event_store_message() => _eventStores[0].Message.ShouldEqual(ResourceNames.EventStoreRuleMessage);
    [Fact] void should_use_the_namespace_message() => _namespaces[0].Message.ShouldEqual(ResourceNames.NamespaceRuleMessage);
    [Fact] void should_not_echo_the_name() => _eventStores[0].Message.ShouldNotContain("a+es+b");
}
