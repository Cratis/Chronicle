// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_opening_registered_legacy_names : given.a_gate
{
    Exception _eventStore;
    Exception _namespace;
    Exception _combination;

    void Establish()
    {
        _gate.MarkRegistered(new EventStoreName("legacy+store"));
        _gate.MarkRegistered(new EventStoreName("legacy+store"), new EventStoreNamespaceName("ns+x"));
        _gate.MarkRegistered(new EventStoreName("chronicle"), new EventStoreNamespaceName("main"));
    }

    void Because()
    {
        _eventStore = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("legacy+store")));
        _namespace = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("legacy+store"), new EventStoreNamespaceName("ns+x")));
        _combination = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("chronicle"), new EventStoreNamespaceName("main")));
    }

    [Fact] void should_allow_the_event_store() => _eventStore.ShouldBeNull();
    [Fact] void should_allow_the_namespace() => _namespace.ShouldBeNull();
    [Fact] void should_still_refuse_the_cluster_combination() => _combination.ShouldBeOfExactType<InvalidResourceName>();
}
