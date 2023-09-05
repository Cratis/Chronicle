// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Routing;

namespace Aksio.Cratis.given;

public class an_http_context : Specification
{
    protected Mock<HttpContext> http_context;
    protected Mock<HttpRequest> request;
    protected Mock<HttpResponse> response;
    protected Mock<IRouteValuesFeature> route_values_feature;
    protected Mock<IFeatureCollection> features;
    protected RouteValueDictionary route_values;
    protected Mock<IServiceProvider> service_provider;
    protected MemoryStream response_body;

    void Establish()
    {
        http_context = new();
        request = new();
        http_context.SetupGet(_ => _.Request).Returns(request.Object);
        request.SetupGet(_ => _.HttpContext).Returns(http_context.Object);
        response = new();
        http_context.SetupGet(_ => _.Response).Returns(response.Object);

        response_body = new MemoryStream();
        response.SetupGet(_ => _.Body).Returns(response_body);
        response.SetupGet(_ => _.HttpContext).Returns(http_context.Object);

        service_provider = new();
        http_context.SetupGet(_ => _.RequestServices).Returns(service_provider.Object);

        route_values_feature = new();
        route_values = new();
        route_values_feature.SetupGet(_ => _.RouteValues).Returns(route_values);
        features = new();
        features.Setup(_ => _.Get<IRouteValuesFeature>()).Returns(route_values_feature.Object);
        http_context.SetupGet(_ => _.Features).Returns(features.Object);
    }

    protected JsonDocument GetJsonFromResponse()
    {
        response_body.Position = 0;
        using var reader = new StreamReader(response_body);
        var json = reader.ReadToEnd();
        return JsonDocument.Parse(json);
    }

    protected T DeserializeResponse<T>()
    {
        response_body.Position = 0;
        using var reader = new StreamReader(response_body);
        var json = reader.ReadToEnd();
        return JsonSerializer.Deserialize<T>(json, Globals.JsonSerializerOptions)!;
    }
}
