// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Aksio.Cratis.AspNetCore.Auditing.for_CausationMiddleware.given;

public class a_causation_middleware_with_required_properties_present : a_causation_middleware
{
    protected const string route = "/some/route";
    protected const string method = "POST";
    protected const string host = "somehost";
    protected const string protocol = "HTTP/1.1";
    protected const string scheme = "https";
    protected const string query = "?some=query&string=here";
    protected const string origin = "Some origin";
    protected const string referer = "Some referer";

    protected const string first_route_value_key = "first";
    protected const string first_route_value_value = "first-value";
    protected const string second_route_value_key = "second";
    protected const string second_route_value_value = "second-value";

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
}
