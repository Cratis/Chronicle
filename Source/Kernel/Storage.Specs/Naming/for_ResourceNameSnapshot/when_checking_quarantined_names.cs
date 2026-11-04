// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameSnapshot;

public class when_checking_quarantined_names : Specification
{
    ResourceNameSnapshot _snapshot;
    ResourceNameStatus _quarantinedStore;
    ResourceNameStatus _namespaceOfQuarantinedStore;
    ResourceNameStatus _quarantinedNamespace;
    ResourceNameStatus _otherNamespace;
    ResourceNameStatus _otherStore;
    ResourceNameStatus _differentCase;

    void Establish()
    {
        _snapshot = new();
        _snapshot.Publish([new EventStoreName("bad")], [(new EventStoreName("shared"), new EventStoreNamespaceName("n1"))]);
    }

    void Because()
    {
        _quarantinedStore = _snapshot.Check("bad");
        _namespaceOfQuarantinedStore = _snapshot.Check("bad", "any");
        _quarantinedNamespace = _snapshot.Check("shared", "n1");
        _otherNamespace = _snapshot.Check("shared", "n2");
        _otherStore = _snapshot.Check("good", "n1");
        _differentCase = _snapshot.Check("BAD");
    }

    [Fact] void should_have_quarantine() => _snapshot.HasQuarantine.ShouldBeTrue();
    [Fact] void should_quarantine_the_store() => _quarantinedStore.ShouldEqual(ResourceNameStatus.Quarantined);
    [Fact] void should_quarantine_every_namespace_of_the_store() => _namespaceOfQuarantinedStore.ShouldEqual(ResourceNameStatus.Quarantined);
    [Fact] void should_quarantine_the_namespace() => _quarantinedNamespace.ShouldEqual(ResourceNameStatus.Quarantined);
    [Fact] void should_not_quarantine_other_namespaces() => _otherNamespace.ShouldEqual(ResourceNameStatus.Available);
    [Fact] void should_not_quarantine_other_stores() => _otherStore.ShouldEqual(ResourceNameStatus.Available);
    [Fact] void should_ignore_case() => _differentCase.ShouldEqual(ResourceNameStatus.Quarantined);
}
