// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_ChronicleClient.when_getting_event_store;

public class with_explicit_namespace : Specification
{
    IEventStoreNamespaceResolver _namespaceResolver;
    EventStoreNamespaceName _explicitNamespace;
    ChronicleOptions _options;
    ChronicleClient _client;

    void Establish()
    {
        _explicitNamespace = "ExplicitNamespace";
        _namespaceResolver = Substitute.For<IEventStoreNamespaceResolver>();

        _options = new ChronicleOptions
        {
            EventStoreNamespaceResolver = _namespaceResolver,
            AutoDiscoverAndRegister = false
        };

        _client = new ChronicleClient(_options);
    }

    void Because() => _ = _client.GetEventStore("TestStore", _explicitNamespace);

    [Fact] void should_not_call_namespace_resolver() => _namespaceResolver.DidNotReceive().Resolve();
}
