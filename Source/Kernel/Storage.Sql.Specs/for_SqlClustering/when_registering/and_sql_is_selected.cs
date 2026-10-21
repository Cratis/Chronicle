// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage.Sql.Cluster;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SqlClustering.when_registering;

public class and_sql_is_selected : Specification
{
    ServiceCollection _services;
    IChronicleBuilder _builder;

    void Establish()
    {
        _services = [];
        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(_services);
        _builder = new ChronicleBuilder(silo, _services, Substitute.For<IConfiguration>());
    }

    void Because() => _builder.WithSql(new ChronicleOptions
    {
        Storage = new() { Type = StorageType.PostgreSql, ConnectionDetails = "Host=localhost;Database=chronicle" },
        Clustering = new() { Type = ClusteringType.Sql }
    });

    [Fact] void should_register_sql_membership() => _services.Count(_ => _.ServiceType == typeof(IMembershipTable) && _.ImplementationType == typeof(SqlMembershipTable)).ShouldEqual(1);
}
