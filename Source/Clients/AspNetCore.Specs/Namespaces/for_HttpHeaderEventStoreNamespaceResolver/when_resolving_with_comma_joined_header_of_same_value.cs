// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.AspNetCore.Namespaces.for_HttpHeaderEventStoreNamespaceResolver;

public class when_resolving_with_comma_joined_header_of_same_value : given.a_resolver_with_tenant_header
{
    EventStoreNamespaceName _result;

    void Establish() => WithHeader("TenantA, TenantA");

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_the_single_namespace() => _result.ShouldEqual((EventStoreNamespaceName)"TenantA");
}
