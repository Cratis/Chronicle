// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Auditing;
using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware.given;

public class a_causation_middleware : Specification
{
    protected ICausationManager _causationManager;

    protected CausationMiddleware _middleware;

    protected HttpContext _httpContext;
    protected HttpRequest _httpRequest;
    protected IHeaderDictionary _httpRequestHeaders;

    protected IDictionary<string, string> _causationProperties;

    void Establish()
    {
        _causationManager = Substitute.For<ICausationManager>();
        _httpContext = Substitute.For<HttpContext>();
        _httpRequest = Substitute.For<HttpRequest>();
        _httpContext.Request.Returns(_httpRequest);

        _httpRequestHeaders = Substitute.For<IHeaderDictionary>();
        _httpRequest.Headers.Returns(_httpRequestHeaders);

        _causationManager
            .When(_ => _.Add(CausationMiddleware.CausationType, Arg.Any<IDictionary<string, string>>()))
            .Do(callInfo => _causationProperties = callInfo.Arg<IDictionary<string, string>>());

        _middleware = new(_causationManager, _ => Task.CompletedTask);
    }
}
