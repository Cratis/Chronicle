// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_cratis_route : given.a_causation_middleware
{
    void Establish()
    {
        _httpRequest.Path.Returns((PathString)"/.cratis/something");
    }

    async Task Because() => await _middleware.InvokeAsync(_httpContext);

    [Fact] void should_not_add_causation() => _causationManager.DidNotReceive().Add(CausationMiddleware.CausationType, Arg.Any<IDictionary<string, string>>());
}
