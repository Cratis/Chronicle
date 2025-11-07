// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.for_ClaimsBasedNamespaceResolver;

public class when_resolving_with_authenticated_user_and_tenant_claim : Specification
{
    ClaimsBasedNamespaceResolver _resolver;
    EventStoreNamespaceName _result;
    EventStoreNamespaceName _expectedNamespace;

    void Establish()
    {
        _expectedNamespace = "tenant-123";
        var claims = new[]
        {
            new Claim("tenant_id", _expectedNamespace.Value)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        ClaimsPrincipal.ClaimsPrincipalSelector = () => principal;
        _resolver = new ClaimsBasedNamespaceResolver();
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_namespace_from_claim() => _result.ShouldEqual(_expectedNamespace);
}
