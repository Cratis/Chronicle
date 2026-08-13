// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_HealthOnlyPortPolicy.given;

public class an_exclusive_dedicated_health_port : Specification
{
    protected const int MainPort = 35000;
    protected const int HealthPort = 8080;
    protected ChronicleOptions _options;

    void Establish() => _options = new ChronicleOptions
    {
        Port = MainPort,
        HealthCheckEndpoint = "/health",
        Health = new Health { Port = HealthPort, Exclusive = true }
    };
}
