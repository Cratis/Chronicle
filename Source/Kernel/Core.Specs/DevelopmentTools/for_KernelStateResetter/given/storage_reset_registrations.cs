// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Setup;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.MongoDB;
using Cratis.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using IDatabase = Cratis.Chronicle.Storage.Sql.IDatabase;

namespace Cratis.Chronicle.DevelopmentTools.for_KernelStateResetter.given;

public class storage_reset_registrations : Specification
{
    protected static ServiceProvider ServicesFor(string storageType, string connectionDetails, bool conventionsFirst)
    {
        var services = new ServiceCollection();
        services.AddTypeDiscovery();
        if (conventionsFirst) services.AddBindingsByConvention().AddSelfBindings();

        var silo = Substitute.For<ISiloBuilder>();
        silo.Services.Returns(services);
        var builder = new ChronicleBuilder(silo, services, Substitute.For<IConfiguration>());
        var options = new ChronicleOptions { Storage = new() { Type = storageType, ConnectionDetails = connectionDetails } };
        if (storageType == StorageType.MongoDB)
        {
            builder.WithMongoDB(options);
        }
        else
        {
            builder.WithSql(options);
        }

        if (!conventionsFirst) services.AddBindingsByConvention().AddSelfBindings();
        if (storageType == StorageType.MongoDB)
        {
            services.RemoveAll<IDatabase>();
            services.AddSingleton(Substitute.For<IMongoDBClientManager>());
        }
        else
        {
            services.RemoveAll<IMongoDBClientManager>();
            services.AddSingleton(Substitute.For<IDatabase>());
        }

        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(options));
        return services.BuildServiceProvider();
    }

    protected static ICanPerformKernelStateReset[] ResolveHandlers(string storageType, string connectionDetails, bool conventionsFirst)
    {
        using var services = ServicesFor(storageType, connectionDetails, conventionsFirst);
        return services.GetServices<ICanPerformKernelStateReset>().ToArray();
    }
}
