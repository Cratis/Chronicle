// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Namespaces.for_SubdomainNamespaceResolver;

public class when_resolving_without_http_context : Specification
{
    SubdomainNamespaceResolver _resolver;
    IHttpContextAccessor _httpContextAccessor;
    EventStoreNamespaceName _result;

    void Establish()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContextAccessor.HttpContext.Returns((HttpContext)null);

        _resolver = new SubdomainNamespaceResolver(_httpContextAccessor);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
