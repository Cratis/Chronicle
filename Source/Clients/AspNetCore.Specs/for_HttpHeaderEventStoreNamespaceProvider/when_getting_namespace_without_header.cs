// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.for_HttpHeaderEventStoreNamespaceProvider;

public class when_getting_namespace_without_header : Specification
{
    HttpHeaderEventStoreNamespaceProvider _provider;
    IHttpContextAccessor _httpContextAccessor;
    IOptions<Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions> _options;
    EventStoreNamespaceName _result;

    void Establish()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        var request = Substitute.For<HttpRequest>();
        var headers = new HeaderDictionary();

        request.Headers.Returns(headers);
        httpContext.Request.Returns(request);
        _httpContextAccessor.HttpContext.Returns(httpContext);

        _options = Substitute.For<IOptions<Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions>>();
        _options.Value.Returns(new Microsoft.AspNetCore.Builder.ChronicleAspNetCoreOptions
        {
            NamespaceHttpHeader = "x-cratis-tenant-id"
        });

        _provider = new HttpHeaderEventStoreNamespaceProvider(_httpContextAccessor, _options);
    }

    void Because() => _result = _provider.GetNamespace();

    [Fact] void should_return_default_namespace() => _result.ShouldEqual(EventStoreNamespaceName.Default);
}
