// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.AspNetCore.TestHost;
namespace Cratis.Chronicle.Integration.Api;

public class ApiWebApplicationFactory(IChronicleSetupFixture fixture, ContentRoot contentRoot) : ChronicleWebApplicationFactory<Program>(fixture, contentRoot)
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        // Overrides the base factory's bare connection string with the out-of-process
        // container's real dev credentials. Configuring ChronicleClientOptions - not
        // ChronicleAspNetCoreOptions - because that's the type AddCratisChronicleClient
        // actually reads; the two are separate IOptions<T> registrations even though
        // ChronicleAspNetCoreOptions derives from it.
        builder.ConfigureTestServices(services => services.Configure<ChronicleClientOptions>(options =>
        {
            options.EventStore = Constants.EventStore;
            options.ConnectionString = new ChronicleConnectionStringBuilder()
                .WithTlsValidationSkipped()
                .WithCredentials("chronicle-dev-client", "chronicle-dev-secret")
                .Build();
        }));
    }
}
