// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;

namespace Cratis.Chronicle.for_ClaimsBasedNamespaceResolver;

public class when_resolving_without_authenticated_user : Specification
{
    ClaimsBasedNamespaceResolver _resolver;
    EventStoreNamespaceName _result;

    void Establish()
    {
        ClaimsPrincipal.ClaimsPrincipalSelector = () => null!;
        _resolver = new ClaimsBasedNamespaceResolver();
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
