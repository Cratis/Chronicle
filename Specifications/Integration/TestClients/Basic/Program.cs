// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis;
using Cratis.Client;
using Cratis.Configuration;
using Aksio.Execution;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Basic;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication
                .CreateBuilder(args)
                .UseCratis(_ => _
                    .MultiTenanted()
                    .ForMicroservice("cfd2f397-3476-4080-885f-feb36878a307", "Basic"));

builder.Host
        .UseMongoDB()
        .ConfigureServices(services => services.AddMongoDBReadModels())
        .UseAksio();

var app = builder.Build();
app.UseRouting();
app.UseCratis();
app.UseAksio();

app.Run();
