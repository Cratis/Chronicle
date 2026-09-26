// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_with_a_route_template : given.a_causation_middleware_with_required_properties_present
{
    const string RawPath = "/customers/jane@example.com";
    const string Template = "/customers/{email}";

    void Establish()
    {
        _httpRequest.Path.Returns((PathString)RawPath);
        _httpContext.Features.Returns(new FeatureCollection());
        _httpContext.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(Template),
            0,
            new EndpointMetadataCollection(),
            "Customers"));
    }

    async Task Because() => await _middleware.InvokeAsync(_httpContext);

    [Fact] void should_record_the_route_template() => _causationProperties[CausationMiddleware.CausationRouteTemplateProperty].ShouldEqual(Template);
    [Fact] void should_preserve_the_raw_path() => _causationProperties[CausationMiddleware.CausationRouteProperty].ShouldEqual(RawPath);
}
