// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_with_all_header_values_present : given.a_causation_middleware_with_required_properties_present
{
    void Establish()
    {
        http_request_headers.Origin.Returns((StringValues)origin);
        http_request_headers.Referer.Returns((StringValues)referer);
    }

    async Task Because() => await _middleware.InvokeAsync(_httpContext);

    [Fact] void should_add_causation() => _causationManager.Received(1).Add(CausationMiddleware.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_add_route_property() => _causationProperties[CausationMiddleware.CausationRouteProperty].ShouldEqual(route);
    [Fact] void should_add_method_property() => _causationProperties[CausationMiddleware.CausationMethodProperty].ShouldEqual(method);
    [Fact] void should_add_host_property() => _causationProperties[CausationMiddleware.CausationHostProperty].ShouldEqual(host);
    [Fact] void should_add_protocol_property() => _causationProperties[CausationMiddleware.CausationProtocolProperty].ShouldEqual(protocol);
    [Fact] void should_add_scheme_property() => _causationProperties[CausationMiddleware.CausationSchemeProperty].ShouldEqual(scheme);
    [Fact] void should_add_query_property() => _causationProperties[CausationMiddleware.CausationQueryProperty].ShouldEqual(query);
    [Fact] void should_add_origin_property() => _causationProperties[CausationMiddleware.CausationOriginProperty].ShouldEqual(origin);
    [Fact] void should_add_referer_property() => _causationProperties[CausationMiddleware.CausationRefererProperty].ShouldEqual(referer);
    [Fact] void should_add_first_route_value_property() => _causationProperties[$"{CausationMiddleware.CausationRouteValuePrefix}:{first_route_value_key}"].ShouldEqual(first_route_value_value);
    [Fact] void should_add_second_route_value_property() => _causationProperties[$"{CausationMiddleware.CausationRouteValuePrefix}:{second_route_value_key}"].ShouldEqual(second_route_value_value);
}
