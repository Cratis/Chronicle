// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Primitives;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware;

public class when_invoking_with_all_header_values_present : given.a_causation_middleware_with_required_properties_present
{
    void Establish()
    {
        _httpRequestHeaders.Origin.Returns((StringValues)_origin);
        _httpRequestHeaders.Referer.Returns((StringValues)_referer);
    }

    async Task Because() => await _middleware.InvokeAsync(_httpContext);

    [Fact] void should_add_causation() => _causationManager.Received(1).Add(CausationMiddleware.CausationType, Arg.Any<IDictionary<string, string>>());
    [Fact] void should_add_route_property() => _causationProperties[CausationMiddleware.CausationRouteProperty].ShouldEqual(_route);
    [Fact] void should_add_method_property() => _causationProperties[CausationMiddleware.CausationMethodProperty].ShouldEqual(_method);
    [Fact] void should_add_host_property() => _causationProperties[CausationMiddleware.CausationHostProperty].ShouldEqual(_host);
    [Fact] void should_add_protocol_property() => _causationProperties[CausationMiddleware.CausationProtocolProperty].ShouldEqual(_protocol);
    [Fact] void should_add_scheme_property() => _causationProperties[CausationMiddleware.CausationSchemeProperty].ShouldEqual(_scheme);
    [Fact] void should_add_query_property() => _causationProperties[CausationMiddleware.CausationQueryProperty].ShouldEqual(_query);
    [Fact] void should_add_origin_property() => _causationProperties[CausationMiddleware.CausationOriginProperty].ShouldEqual(_origin);
    [Fact] void should_add_referer_property() => _causationProperties[CausationMiddleware.CausationRefererProperty].ShouldEqual(_referer);
    [Fact] void should_add_first_route_value_property() => _causationProperties[$"{CausationMiddleware.CausationRouteValuePrefix}:{_firstRouteValueKey}"].ShouldEqual(_firstRouteValueValue);
    [Fact] void should_add_second_route_value_property() => _causationProperties[$"{CausationMiddleware.CausationRouteValuePrefix}:{_secondRouteValueKey}"].ShouldEqual(_secondRouteValueValue);
}
