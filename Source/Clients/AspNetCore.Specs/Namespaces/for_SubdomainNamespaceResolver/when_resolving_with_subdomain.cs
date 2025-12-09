// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Namespaces.for_SubdomainNamespaceResolver;

public class when_resolving_with_subdomain : Specification
{
    SubdomainNamespaceResolver _resolver;
    IHttpContextAccessor _httpContextAccessor;
    EventStoreNamespaceName _result;
    EventStoreNamespaceName _expectedNamespace;

    void Establish()
    {
        _expectedNamespace = "tenant123";
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();

        request.Host.Returns(new HostString($"{_expectedNamespace.Value}.example.com"));
        httpContext.Request.Returns(request);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _resolver = new SubdomainNamespaceResolver(_httpContextAccessor);
    }

    void Because() => _result = _resolver.Resolve();

    [Fact] void should_return_namespace_from_subdomain() => _result.ShouldEqual(_expectedNamespace);
}
