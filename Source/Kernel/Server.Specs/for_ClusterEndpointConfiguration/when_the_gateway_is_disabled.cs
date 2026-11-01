// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterEndpointConfiguration;

public class when_the_gateway_is_disabled : given.a_silo
{
    EndpointOptions _localhost;
    EndpointOptions _mongo;
    EndpointOptions _sql;
    void Because()
    {
        _localhost = Configure(new() { GatewayEnabled = false });
        _mongo = Configure(new() { Type = ClusteringType.MongoDB, GatewayEnabled = false });
        _sql = Configure(new() { Type = ClusteringType.Sql, GatewayEnabled = false });
    }

    [Fact] void should_disable_the_localhost_gateway() => _localhost.GatewayPort.ShouldEqual(0);
    [Fact] void should_disable_the_mongodb_gateway() => _mongo.GatewayPort.ShouldEqual(0);
    [Fact] void should_disable_the_sql_gateway() => _sql.GatewayPort.ShouldEqual(0);
    [Fact] void should_keep_the_silo_port() => _mongo.SiloPort.ShouldEqual(11111);
}
