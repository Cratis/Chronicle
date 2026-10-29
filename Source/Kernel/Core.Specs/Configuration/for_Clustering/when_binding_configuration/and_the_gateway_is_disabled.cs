// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Configuration;

namespace Cratis.Chronicle.Configuration.for_Clustering.when_binding_configuration;

public class and_the_gateway_is_disabled : Specification
{
    Clustering _result;
    void Because() => _result = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["clustering:gatewayEnabled"] = "false",
        ["clustering:gatewayPort"] = "31000"
    }).Build().GetSection("clustering").Get<Clustering>()!;

    [Fact] void should_bind_the_switch() => _result.GatewayEnabled.ShouldBeFalse();
    [Fact] void should_preserve_the_configured_port() => _result.GatewayPort.ShouldEqual(31000);
    [Fact] void should_enable_the_gateway_by_default() => new Clustering().GatewayEnabled.ShouldBeTrue();
}
