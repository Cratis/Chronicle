// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_DefaultEventStoreNamespaceResolver;

public class when_resolving : Specification
{
    DefaultEventStoreNamespaceResolver _resolver;
    EventStoreNamespaceName _result;

    void Establish() => _resolver = new DefaultEventStoreNamespaceResolver();

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
