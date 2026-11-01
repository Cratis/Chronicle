// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Orleans.Configuration;

namespace Cratis.Chronicle.Server.for_ClusterEndpointConfiguration;

public class when_no_endpoint_overrides_are_set : given.a_silo
{
    EndpointOptions _localhost;
    EndpointOptions _mongo;
    EndpointOptions _sql;
    EndpointOptions _originalLocalhost;
    EndpointOptions _originalCluster;
    void Because()
    {
        _localhost = Configure(new());
        _mongo = Configure(new() { Type = ClusteringType.MongoDB });
        _sql = Configure(new() { Type = ClusteringType.Sql });
        _originalLocalhost = ConfigureOrleans(silo => silo.UseLocalhostClustering(11111, 30000, serviceId: "chronicle", clusterId: "chronicle"));
        _originalCluster = ConfigureOrleans(silo => silo.ConfigureEndpoints(11111, 30000));
    }

    [Fact] void should_preserve_localhost_endpoints() => Endpoints(_localhost).ShouldEqual(Endpoints(_originalLocalhost));
    [Fact] void should_preserve_mongodb_endpoints() => Endpoints(_mongo).ShouldEqual(Endpoints(_originalCluster));
    [Fact] void should_preserve_sql_endpoints() => Endpoints(_sql).ShouldEqual(Endpoints(_originalCluster));
    [Fact] void should_keep_the_default_gateway() => _localhost.GatewayPort.ShouldEqual(30000);
    static object Endpoints(EndpointOptions options) => (options.AdvertisedIPAddress, options.SiloPort, options.GatewayPort, options.SiloListeningEndpoint, options.GatewayListeningEndpoint);
}
