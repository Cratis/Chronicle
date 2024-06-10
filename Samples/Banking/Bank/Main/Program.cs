// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Main;
using Aksio.Cratis;

// Force invariant culture for the Kernel
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args)
                .UseChronicle(_ => _
                    .MultiTenanted()
                    .WithSoftwareVersion("1.0.0", "1234567890")
                    .IdentifiedAs("Bank")
                    //.UseIdentityProvider<CustomIdentityProvider>()
                    .UseAspNetCoreIdentityProvider()
                    .ForMicroservice("eaf02867-79b9-4967-be67-3e93cee7c601", "Bank"));
builder.Host
        .UseMongoDB()
        .ConfigureServices(services => services.AddMongoDBReadModels())
        .UseAksio();

var app = builder.Build();
app.UseRouting();
app.UseChronicle();
app.UseAksio();

app.Run();
