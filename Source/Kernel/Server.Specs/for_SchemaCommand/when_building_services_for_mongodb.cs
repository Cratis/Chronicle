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

    void Because()
    {
        _provider = SchemaCommandRunner.BuildServices(
            new ChronicleOptions { Storage = new() { Type = StorageType.MongoDB, ConnectionDetails = "mongodb://localhost:27017" } },
            new ConfigurationBuilder().Build(),
            _ => { });
        _provisioner = _provider.GetRequiredService<IMongoDBIndexProvisioner>();
    }

    [Fact] void should_resolve_the_index_provisioner_without_a_host() => _provisioner.ShouldNotBeNull();
}
