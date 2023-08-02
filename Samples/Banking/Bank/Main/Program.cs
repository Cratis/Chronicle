// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

// Force invariant culture for the Kernel
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args)
                .UseCratis(_ => _
                    .MultiTenanted()
                    .ForMicroservice("eaf02867-79b9-4967-be67-3e93cee7c601", "Bank"));
builder.Host
        .UseMongoDB()
        .ConfigureServices(services => services.AddMongoDBReadModels())
        .UseAksio();

var app = builder.Build();
app.UseRouting();
app.UseCratis();
app.UseAksio();

app.Run();
