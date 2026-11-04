// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;
using Cratis.Chronicle.Storage.Naming;
using Orleans.TestKit;

namespace Cratis.Chronicle.Namespaces.for_Namespaces.when_getting_all;

public class and_a_namespace_is_quarantined : given.a_namespaces_grain
{
    IEnumerable<EventStoreNamespaceName> _result;

    void Establish()
    {
        _state.Namespaces.Add(new NamespaceState("Default", DateTimeOffset.UtcNow));
        _state.Namespaces.Add(new NamespaceState("Tenant", DateTimeOffset.UtcNow));
        var snapshot = ResourceNameSnapshot.NotReady();
        snapshot.Publish([], [(_eventStore, new EventStoreNamespaceName("Tenant"))]);
        _silo.AddService<IResourceNameSnapshot>(snapshot);
    }

    async Task Because() => _result = await _namespaces.GetAll();

    [Fact] void should_leave_out_the_quarantined_namespace() => _result.ShouldEqual([new EventStoreNamespaceName("Default")]);
}
