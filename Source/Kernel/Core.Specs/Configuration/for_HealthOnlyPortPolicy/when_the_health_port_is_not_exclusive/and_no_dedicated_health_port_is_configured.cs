// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.when_the_health_port_is_not_exclusive;

public class and_no_dedicated_health_port_is_configured : Specification
{
    const int MainPort = 35000;
    ChronicleOptions _options;
    bool _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = MainPort,
        Health = new Health { Exclusive = true }
    };

    void Because() => _result = HealthOnlyPortPolicy.ShouldReject(_options, MainPort, "/api/event-store");

    [Fact] void should_serve_the_request() => _result.ShouldBeFalse();
}
