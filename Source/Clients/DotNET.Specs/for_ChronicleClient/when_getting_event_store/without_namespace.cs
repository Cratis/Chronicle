// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

public class without_namespace : Specification
{
    IEventStoreNamespaceProvider _namespaceProvider;
    EventStoreNamespaceName _customNamespace;
    ChronicleOptions _options;
    ChronicleClient _client;

    void Establish()
    {
        _customNamespace = "CustomNamespace";
        _namespaceProvider = Substitute.For<IEventStoreNamespaceProvider>();
        _namespaceProvider.GetNamespace().Returns(_customNamespace);

        _options = new ChronicleOptions
        {
            EventStoreNamespaceProvider = _namespaceProvider,
            AutoDiscoverAndRegister = false
        };

        _client = new ChronicleClient(_options);
    }

    void Because() => _ = _client.GetEventStore("TestStore");

    [Fact] void should_call_namespace_provider() => _namespaceProvider.Received(1).GetNamespace();
}
