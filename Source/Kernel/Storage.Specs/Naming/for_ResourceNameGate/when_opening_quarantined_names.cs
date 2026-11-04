// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameGate;

public class when_opening_quarantined_names : given.a_gate
{
    Exception _store;
    Exception _namespace;
    Exception _available;
    Exception _hostileAndQuarantined;
    Exception _availableOnly;

    void Establish()
    {
        _gate.MarkRegistered(new EventStoreName("bad+store"));
        _snapshot.Publish([new EventStoreName("bad"), new EventStoreName("bad+store")], [(new EventStoreName("shared"), new EventStoreNamespaceName("n1"))]);
    }

    void Because()
    {
        _store = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("bad")));
        _namespace = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("shared"), new EventStoreNamespaceName("n1")));
        _available = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("shared"), new EventStoreNamespaceName("n2")));
        _hostileAndQuarantined = Catch.Exception(() => _gate.EnsureCanOpen(new EventStoreName("es")));
        _availableOnly = Catch.Exception(() => _gate.EnsureAvailable(new EventStoreName("bad+store")));
    }

    [Fact] void should_refuse_the_store() => _store.ShouldBeOfExactType<ResourceQuarantined>();
    [Fact] void should_refuse_the_namespace() => _namespace.ShouldBeOfExactType<ResourceQuarantined>();
    [Fact] void should_allow_other_namespaces() => _available.ShouldBeNull();
    [Fact] void should_report_the_rule_before_the_quarantine() => _hostileAndQuarantined.ShouldBeOfExactType<InvalidResourceName>();
    [Fact] void should_refuse_a_registered_quarantined_store_without_the_rule() => _availableOnly.ShouldBeOfExactType<ResourceQuarantined>();
    [Fact] void should_use_the_quarantine_message() => _store.Message.ShouldEqual(ResourceNames.QuarantinedMessage);
}
