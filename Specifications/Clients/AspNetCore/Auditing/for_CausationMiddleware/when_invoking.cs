// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aksio.Cratis.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking : given.a_causation_middleware
{
    const string route = "/some/route";
    const string method = "POST";
    const string host = "somehost";
    const string protocol = "HTTP/1.1";
    const string scheme = "https";
    const string query = "?some=query&string=here";

    const string first_route_value_key = "first";
    const string first_route_value_value = "first-value";
    const string second_route_value_key = "second";
    const string second_route_value_value = "second-value";


    void Establish()
    {
        http_request.SetupGet(_ => _.Path).Returns(route);
        http_request.SetupGet(_ => _.Method).Returns(method);
        http_request.SetupGet(_ => _.Host).Returns(new HostString(host));
        http_request.SetupGet(_ => _.Protocol).Returns(protocol);
        http_request.SetupGet(_ => _.Scheme).Returns(scheme);
        http_request.SetupGet(_ => _.QueryString).Returns(new QueryString(query));
        http_request.SetupGet(_ => _.RouteValues).Returns(new RouteValueDictionary(new Dictionary<string, object>
        {
            { first_route_value_key, first_route_value_value },
            { second_route_value_key, second_route_value_value }
        }));
    }

    async Task Because() => await middleware.InvokeAsync(http_context.Object);

    [Fact] void should_add_causation() => causation_manager.Verify(_ => _.Add(CausationMiddleware.CausationType, IsAny<IDictionary<string, string>>()), Once);
    [Fact] void should_add_route_property() => causation_properties[CausationMiddleware.CausationRouteProperty].ShouldEqual(route);
    [Fact] void should_add_method_property() => causation_properties[CausationMiddleware.CausationMethodProperty].ShouldEqual(method);
    [Fact] void should_add_host_property() => causation_properties[CausationMiddleware.CausationHostProperty].ShouldEqual(host);
    [Fact] void should_add_protocol_property() => causation_properties[CausationMiddleware.CausationProtocolProperty].ShouldEqual(protocol);
    [Fact] void should_add_scheme_property() => causation_properties[CausationMiddleware.CausationSchemeProperty].ShouldEqual(scheme);
    [Fact] void should_add_query_property() => causation_properties[CausationMiddleware.CausationQueryProperty].ShouldEqual(query);
    [Fact] void should_add_first_route_value_property() => causation_properties[$"{CausationMiddleware.CausationRouteValuePrefix}:{first_route_value_key}"].ShouldEqual(first_route_value_value);
    [Fact] void should_add_second_route_value_property() => causation_properties[$"{CausationMiddleware.CausationRouteValuePrefix}:{second_route_value_key}"].ShouldEqual(second_route_value_value);
}
