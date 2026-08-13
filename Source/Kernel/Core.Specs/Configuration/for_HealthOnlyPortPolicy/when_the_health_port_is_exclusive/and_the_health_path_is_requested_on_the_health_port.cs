// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_exclusive;

public class and_the_health_path_is_requested_on_the_health_port : given.an_exclusive_dedicated_health_port
{
    bool _result;

    void Because() => _result = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/health");

    [Fact] void should_serve_the_request() => _result.ShouldBeFalse();
}
