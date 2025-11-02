// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

public class without_namespace : Specification
{
    IEventStoreNamespaceResolver _namespaceResolver;
    EventStoreNamespaceName _customNamespace;
    ChronicleOptions _options;
    ChronicleClient _client;

    void Establish()
    {
        _customNamespace = "CustomNamespace";
        _namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();
        _namespaceResolver.Resolve().Returns(_customNamespace);

        _options = new ChronicleOptions
        {
            EventStoreNamespaceResolver = _namespaceResolver,
            AutoDiscoverAndRegister = false
        };

        _client = new ChronicleClient(_options);
    }

    void Because() => _ = _client.GetEventStore("TestStore");

    [Fact] void should_call_namespace_resolver() => _namespaceResolver.Received(1).Resolve();
}
