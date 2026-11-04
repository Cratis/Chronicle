// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Naming.for_ResourceNameSnapshot;

public class when_checking_a_namespace_while_a_quarantine_exists : Specification
{
    const int Iterations = 2000;

    ResourceNameSnapshot _snapshot;
    long _namespaceCheck;
    long _storeCheck;
    long _quarantinedCheck;

    void Establish()
    {
        _snapshot = new();
        _snapshot.Publish([new EventStoreName("bad")], [(new EventStoreName("shared"), new EventStoreNamespaceName("n1"))]);
    }

    void Because()
    {
        _namespaceCheck = AllocationProbe.Measure(Iterations, () => _snapshot.Check("good", "n2"));
        _storeCheck = AllocationProbe.Measure(Iterations, () => _snapshot.Check("good"));
        _quarantinedCheck = AllocationProbe.Measure(Iterations, () => _snapshot.Check("SHARED", "N1"));
    }

    [Fact] void should_not_allocate_when_checking_an_available_namespace() => _namespaceCheck.ShouldEqual(0);
    [Fact] void should_not_allocate_when_checking_an_available_store() => _storeCheck.ShouldEqual(0);
    [Fact] void should_not_allocate_when_checking_a_quarantined_namespace() => _quarantinedCheck.ShouldEqual(0);
}
