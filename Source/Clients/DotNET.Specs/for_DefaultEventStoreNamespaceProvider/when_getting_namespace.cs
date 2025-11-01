// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_DefaultEventStoreNamespaceProvider;

public class when_getting_namespace : Specification
{
    DefaultEventStoreNamespaceProvider _provider;
    EventStoreNamespaceName _result;

    void Establish() => _provider = new DefaultEventStoreNamespaceProvider();

    void Because() => _result = _provider.GetNamespace();

    [Fact] void should_return_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
