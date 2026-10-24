// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.given;

public class a_schema_provisioner : for_Database.given.a_sqlite_database
{
    readonly List<ServiceProvider> _providers = [];
    protected ISchemaProvisioner _provisioner;
    protected Database _schemaDatabase;
    protected ServiceProvider _services;
    protected static SchemaScope Scope => new(["orders"], ["production"]);

    void Establish()
    {
        _services = CreateServices();
        _provisioner = _services.GetRequiredService<ISchemaProvisioner>();
        _schemaDatabase = _services.GetRequiredService<Database>();
    }

    protected ServiceProvider CreateServices(SchemaMode mode = SchemaMode.Migrate, bool createIfMissing = true)
    {
        var options = new ChronicleOptions
        {
            Storage = new()
            {
                Type = StorageType.Sqlite,
                ConnectionDetails = _connectionString,
                SchemaMode = mode,
                CreateIfMissing = createIfMissing
            }
        };
        var provider = schema_services.Create(options);
        _providers.Add(provider);
        return provider;
    }

    void Destroy()
    {
        foreach (var provider in _providers)
        {
            provider.Dispose();
        }
    }
}
