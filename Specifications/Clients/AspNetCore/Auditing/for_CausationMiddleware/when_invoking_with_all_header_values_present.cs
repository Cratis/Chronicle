// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_with_all_header_values_present : given.a_causation_middleware_with_required_properties_present
{
    void Establish()
    {
        http_request_headers.SetupGet(_ => _.Origin).Returns(origin);
        http_request_headers.SetupGet(_ => _.Referer).Returns(referer);
    }

    async Task Because() => await middleware.InvokeAsync(http_context.Object);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(CausationMiddleware.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_add_route_property() => causation_properties[CausationMiddleware.CausationRouteProperty].ShouldEqual(route);
    [Fact] void should_add_method_property() => causation_properties[CausationMiddleware.CausationMethodProperty].ShouldEqual(method);
    [Fact] void should_add_host_property() => causation_properties[CausationMiddleware.CausationHostProperty].ShouldEqual(host);
    [Fact] void should_add_protocol_property() => causation_properties[CausationMiddleware.CausationProtocolProperty].ShouldEqual(protocol);
    [Fact] void should_add_scheme_property() => causation_properties[CausationMiddleware.CausationSchemeProperty].ShouldEqual(scheme);
    [Fact] void should_add_query_property() => causation_properties[CausationMiddleware.CausationQueryProperty].ShouldEqual(query);
    [Fact] void should_add_origin_property() => causation_properties[CausationMiddleware.CausationOriginProperty].ShouldEqual(origin);
    [Fact] void should_add_referer_property() => causation_properties[CausationMiddleware.CausationRefererProperty].ShouldEqual(referer);
    [Fact] void should_add_first_route_value_property() => causation_properties[$"{CausationMiddleware.CausationRouteValuePrefix}:{first_route_value_key}"].ShouldEqual(first_route_value_value);
    [Fact] void should_add_second_route_value_property() => causation_properties[$"{CausationMiddleware.CausationRouteValuePrefix}:{second_route_value_key}"].ShouldEqual(second_route_value_value);
}
