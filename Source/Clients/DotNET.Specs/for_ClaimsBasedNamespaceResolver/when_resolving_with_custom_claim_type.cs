// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.for_ClaimsBasedNamespaceResolver;

public class when_resolving_with_custom_claim_type : Specification
{
    ClaimsBasedNamespaceResolver _resolver;
    EventStoreNamespaceName _result;
    EventStoreNamespaceName _expectedNamespace;

    void Establish()
    {
        _expectedNamespace = "custom-tenant";
        var claims = new[]
        {
            new Claim("custom_claim", _expectedNamespace.Value)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        ClaimsPrincipal.ClaimsPrincipalSelector = () => principal;
        _resolver = new ClaimsBasedNamespaceResolver("custom_claim");
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_namespace_from_custom_claim() => _result.ShouldEqual(_expectedNamespace);
}
