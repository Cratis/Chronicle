// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Auditing;
using Microsoft.AspNetCore.Http;

namespace Aksio.Cratis.AspNetCore.Auditing.for_CausationMiddleware.given;

public class a_causation_middleware : Specification
{
    protected Mock<ICausationManager> causation_manager;

    protected CausationMiddleware middleware;

    protected Mock<HttpContext> http_context;
    protected Mock<HttpRequest> http_request;
    protected Mock<IHeaderDictionary> http_request_headers;

    protected IDictionary<string, string> causation_properties;

    void Establish()
    {
        causation_manager = new();
        http_context = new();
        http_request = new();
        http_context.Setup(_ => _.Request).Returns(http_request.Object);

        http_request_headers = new();
        http_request.SetupGet(_ => _.Headers).Returns(http_request_headers.Object);

        causation_manager
            .Setup(_ => _.Add(CausationMiddleware.CausationType, IsAny<IDictionary<string, string>>()))
            .Callback((CausationType _, IDictionary<string, string> properties) => causation_properties = properties);

        middleware = new(causation_manager.Object, _ => Task.CompletedTask);
    }
}
