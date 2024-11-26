// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware.given;

public class a_causation_middleware_with_required_properties_present : a_causation_middleware
{
    protected const string _route = "/some/route";
    protected const string _method = "POST";
    protected const string _host = "somehost";
    protected const string _protocol = "HTTP/1.1";
    protected const string _scheme = "https";
    protected const string _query = "?some=query&string=here";
    protected const string _origin = "Some origin";
    protected const string _referer = "Some referer";

    protected const string _firstRouteValueKey = "first";
    protected const string _firstRouteValueValue = "first-value";
    protected const string _secondRouteValueKey = "second";
    protected const string _secondRouteValueValue = "second-value";

    void Establish()
    {
        _httpRequest.Path.Returns((PathString)_route);
        _httpRequest.Method.Returns(_method);
        _httpRequest.Host.Returns(new HostString(_host));
        _httpRequest.Protocol.Returns(_protocol);
        _httpRequest.Scheme.Returns(_scheme);
        _httpRequest.QueryString.Returns(new QueryString(_query));
        _httpRequest.RouteValues.Returns(new RouteValueDictionary(new Dictionary<string, object>
        {
            { _firstRouteValueKey, _firstRouteValueValue },
            { _secondRouteValueKey, _secondRouteValueValue }
        }));
    }
}
