// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.AspNetCore.Namespaces.for_HttpHeaderEventStoreNamespaceResolver;

public class when_resolving_with_header_naming_different_namespaces : given.a_resolver_with_tenant_header
{
    Exception _error;

    void Establish() => WithHeader(new StringValues(["TenantA", "TenantB"]));

    void Because() => _error = Catch.Exception(() => _resolver.Resolve());

    [Fact] void should_reject_the_ambiguous_header() => _error.ShouldBeOfExactType<AmbiguousNamespaceHttpHeader>();
}
