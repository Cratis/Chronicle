// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Cratis.Chronicle.AspNetCore.Auditing.for_CausationMiddleware.given;

public class a_causation_middleware_with_required_properties_present : a_causation_middleware
{
    protected const string Route = "/some/route";
    protected const string Method = "POST";
    protected const string Host = "somehost";
    protected const string Protocol = "HTTP/1.1";
    protected const string Scheme = "https";
    protected const string Query = "?some=query&string=here";
    protected const string Origin = "Some origin";
    protected const string Referer = "Some referer";

    protected const string FirstRouteValueKey = "first";
    protected const string FirstRouteValueValue = "first-value";
    protected const string SecondRouteValueKey = "second";
    protected const string SecondRouteValueValue = "second-value";

    void Establish()
    {
        _httpRequest.Path.Returns((PathString)Route);
        _httpRequest.Method.Returns(Method);
        _httpRequest.Host.Returns(new HostString(Host));
        _httpRequest.Protocol.Returns(Protocol);
        _httpRequest.Scheme.Returns(Scheme);
        _httpRequest.QueryString.Returns(new QueryString(Query));
        _httpRequest.RouteValues.Returns(new RouteValueDictionary(new Dictionary<string, object?>
        {
            { FirstRouteValueKey, FirstRouteValueValue },
            { SecondRouteValueKey, SecondRouteValueValue }
        }));
    }
}
