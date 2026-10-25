// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB.Schema;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_SchemaCommand;

public class when_building_services_for_sqlite : Specification
{
    ServiceProvider _provider;
    IConfiguration _configuration;

    void Establish() => _configuration = new ConfigurationBuilder().Build();

    void Because() => _provider = SchemaCommandRunner.BuildServices(
        new ChronicleOptions { Storage = new() { Type = StorageType.Sqlite, ConnectionDetails = "Data Source=:memory:" } },
        _configuration,
        _ => { });

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_sql_provisioner_without_a_host() => _provider.GetRequiredService<ISchemaProvisioner>().ShouldNotBeNull();
    [Fact] void should_not_register_the_mongodb_provisioner() => _provider.GetService<IMongoDBIndexProvisioner>().ShouldBeNull();
    [Fact] void should_retain_the_supplied_configuration() => _provider.GetRequiredService<IConfiguration>().ShouldBeSame(_configuration);
}
