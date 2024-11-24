// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware.given;

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
        _httpRequest.Path.Returns((PathString)route);
        _httpRequest.Method.Returns(method);
        _httpRequest.Host.Returns(new HostString(host));
        _httpRequest.Protocol.Returns(protocol);
        _httpRequest.Scheme.Returns(scheme);
        _httpRequest.QueryString.Returns(new QueryString(query));
        _httpRequest.RouteValues.Returns(new RouteValueDictionary(new Dictionary<string, object>
        {
            { first_route_value_key, first_route_value_value },
            { second_route_value_key, second_route_value_value }
        }));
    }
}
