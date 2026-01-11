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
        builder.ConfigureTestServices(services => services.Configure<ChronicleAspNetCoreOptions>(options =>
        {
            options.EventStore = Constants.EventStore;
            options.ConnectionString = new ChronicleConnectionStringBuilder().WithTlsDisabled().Build();
        }));
    }
}
