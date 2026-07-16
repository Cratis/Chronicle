// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Configuration.for_ChronicleOptions.when_resolving_dedicated_health_port;

public class and_health_port_equals_main_port : Specification
{
    ChronicleOptions _options;
    int? _result;

    void Establish() => _options = new ChronicleOptions
    {
        Port = 35000,
        Health = new Health { Port = 35000 }
    };

    void Because() => _result = _options.DedicatedHealthPort;

    [Fact] void should_serve_health_on_the_main_port() => _result.ShouldBeNull();
}
