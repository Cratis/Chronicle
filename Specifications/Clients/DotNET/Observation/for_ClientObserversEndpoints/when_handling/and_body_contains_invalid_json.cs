// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text;

namespace Aksio.Cratis.Observation.for_ClientObserversEndpoints.when_handling;

public class and_body_contains_invalid_json : Cratis.given.an_http_context
{
    void Establish()
    {
        route_values.Add("observerId", Guid.NewGuid().ToString());
        request.SetupGet(_ => _.Body).Returns(new MemoryStream(Encoding.UTF8.GetBytes("{")));
    }

    Task Because() => ClientObserversEndpoints.Handler(http_context.Object);

    [Fact] void should_return_bad_request() => response.VerifySet(_ => _.StatusCode = (int)HttpStatusCode.BadRequest);
}
