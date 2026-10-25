// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB.Schema;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_SchemaCommand;

public class when_building_services_for_mongodb : Specification
{
    ServiceProvider _provider;
    IMongoDBIndexProvisioner _provisioner;
    IConfiguration _configuration;

    void Establish() => _configuration = new ConfigurationBuilder().Build();

    void Because()
    {
        _provider = SchemaCommandRunner.BuildServices(
            new ChronicleOptions { Storage = new() { Type = StorageType.MongoDB, ConnectionDetails = "mongodb://localhost:27017" } },
            _configuration,
            _ => { });
        _provisioner = _provider.GetRequiredService<IMongoDBIndexProvisioner>();
    }

    void Destroy() => _provider.Dispose();

    [Fact] void should_resolve_the_index_provisioner_without_a_host() => _provisioner.ShouldNotBeNull();
    [Fact] void should_retain_the_supplied_configuration() => _provider.GetRequiredService<IConfiguration>().ShouldBeSame(_configuration);
}
