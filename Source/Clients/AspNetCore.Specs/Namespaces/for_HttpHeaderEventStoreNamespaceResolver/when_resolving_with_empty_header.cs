// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Namespaces.for_HttpHeaderEventStoreNamespaceResolver;

public class when_resolving_with_empty_header : given.a_resolver_with_tenant_header
{
    EventStoreNamespaceName _result;

    void Establish() => WithHeader(" , ");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
