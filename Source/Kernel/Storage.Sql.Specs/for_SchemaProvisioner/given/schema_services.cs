// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Storage.Sql.Schema;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaProvisioner.given;

public static class schema_services
{
    public static ServiceProvider Create(ChronicleOptions options)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IOptions<ChronicleOptions>>(Options.Create(options));
        SqlSchemaServices.Add(services, options);
        return services.BuildServiceProvider();
    }
}
