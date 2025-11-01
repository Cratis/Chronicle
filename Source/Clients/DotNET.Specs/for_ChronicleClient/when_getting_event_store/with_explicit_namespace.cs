// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

public class with_explicit_namespace : Specification
{
    IEventStoreNamespaceProvider _namespaceProvider;
    EventStoreNamespaceName _explicitNamespace;

    void Establish()
    {
        _explicitNamespace = "ExplicitNamespace";
        _namespaceProvider = Substitute.For<IEventStoreNamespaceProvider>();
    }

    void Because()
    {
        var options = new ChronicleOptions
        {
            EventStoreNamespaceProvider = _namespaceProvider,
            AutoDiscoverAndRegister = false
        };

        var client = new ChronicleClient(options);
        _ = client.GetEventStore("TestStore", _explicitNamespace);
    }

    [Fact] void should_not_call_namespace_provider() => _namespaceProvider.DidNotReceive().GetNamespace();
}
