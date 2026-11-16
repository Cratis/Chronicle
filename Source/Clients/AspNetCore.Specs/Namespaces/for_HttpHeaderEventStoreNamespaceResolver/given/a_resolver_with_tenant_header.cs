// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.AspNetCore.Namespaces.for_HttpHeaderEventStoreNamespaceResolver.given;

public class a_resolver_with_tenant_header : Specification
{
    protected const string HeaderName = "x-cratis-tenant-id";

    protected HttpHeaderEventStoreNamespaceResolver _resolver;

    protected void WithHeader(StringValues values)
    {
        var request = Substitute.For<HttpRequest>();
        request.Headers.Returns(new HeaderDictionary { { HeaderName, values } });
        var httpContext = Substitute.For<HttpContext>();
        httpContext.Request.Returns(request);
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        httpContextAccessor.HttpContext.Returns(httpContext);

        var options = Substitute.For<IOptions<Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions>>();
        options.Value.Returns(new Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions { NamespaceHttpHeader = HeaderName });

        _resolver = new HttpHeaderEventStoreNamespaceResolver(httpContextAccessor, options);
    }
}
