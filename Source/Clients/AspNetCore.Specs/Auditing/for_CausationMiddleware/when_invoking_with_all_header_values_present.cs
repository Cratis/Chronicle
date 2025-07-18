// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_with_all_header_values_present : given.a_causation_middleware_with_required_properties_present
{
    void Establish()
    {
        _httpRequestHeaders.Origin.Returns((StringValues)Origin);
        _httpRequestHeaders.Referer.Returns((StringValues)Referer);
    }

    async Task Because() => await _middleware.InvokeAsync(_httpContext);

    [Fact] void should_add_causation() => _causationManager.Received(1).Add(CausationMiddleware.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_add_route_property() => _causationProperties[CausationMiddleware.CausationRouteProperty].ShouldEqual(Route);
    [Fact] void should_add_method_property() => _causationProperties[CausationMiddleware.CausationMethodProperty].ShouldEqual(Method);
    [Fact] void should_add_host_property() => _causationProperties[CausationMiddleware.CausationHostProperty].ShouldEqual(Host);
    [Fact] void should_add_protocol_property() => _causationProperties[CausationMiddleware.CausationProtocolProperty].ShouldEqual(Protocol);
    [Fact] void should_add_scheme_property() => _causationProperties[CausationMiddleware.CausationSchemeProperty].ShouldEqual(Scheme);
    [Fact] void should_add_query_property() => _causationProperties[CausationMiddleware.CausationQueryProperty].ShouldEqual(Query);
    [Fact] void should_add_origin_property() => _causationProperties[CausationMiddleware.CausationOriginProperty].ShouldEqual(Origin);
    [Fact] void should_add_referer_property() => _causationProperties[CausationMiddleware.CausationRefererProperty].ShouldEqual(Referer);
    [Fact] void should_add_first_route_value_property() => _causationProperties[$"{CausationMiddleware.CausationRouteValuePrefix}:{FirstRouteValueKey}"].ShouldEqual(FirstRouteValueValue);
    [Fact] void should_add_second_route_value_property() => _causationProperties[$"{CausationMiddleware.CausationRouteValuePrefix}:{SecondRouteValueKey}"].ShouldEqual(SecondRouteValueValue);
}
