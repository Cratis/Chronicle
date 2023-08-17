// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_cratis_route : given.a_causation_middleware
{
    void Establish()
    {
        http_request.SetupGet(_ => _.Path).Returns("/.cratis/something");
    }

    async Task Because() => await middleware.InvokeAsync(http_context.Object);

    [Fact] void should_not_add_causation() => causation_manager.Verify(_ => _.Add(CausationMiddleware.CausationType, IsAny<IDictionary<string, string>>()), Never);
}
