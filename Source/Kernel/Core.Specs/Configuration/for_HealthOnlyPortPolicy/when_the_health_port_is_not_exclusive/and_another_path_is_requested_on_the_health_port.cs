// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_not_exclusive;

public class and_another_path_is_requested_on_the_health_port : Specification
{
    const int HealthPort = 8080;
    ChronicleOptions _options;
    bool _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        Health = new Health { Port = HealthPort }
    };

    void Because() => _result = HealthOnlyPortPolicy.ShouldReject(_options, HealthPort, "/api/event-store");

    [Fact] void should_serve_the_request() => _result.ShouldBeFalse();
}
