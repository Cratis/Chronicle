// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;

namespace Aksio.Cratis.Observation.for_ClientObserversEndpoints.when_handling;

public class and_observer_id_is_not_string : Cratis.given.an_http_context
{
    void Establish() => route_values.Add("observerId", 42);

    Task Because() => ClientObserversEndpoints.Handler(http_context.Object);

    [Fact] void should_return_bad_request() => response.VerifySet(_ => _.StatusCode = (int)HttpStatusCode.BadRequest);
}
