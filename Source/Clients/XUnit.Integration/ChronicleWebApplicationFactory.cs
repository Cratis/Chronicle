// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.XUnit.Integration;

/// <summary>
/// Represents a web application factory for integration tests.
/// </summary>
/// <param name="fixture">The <see cref="IChronicleSetupFixture"/>.</param>
/// <param name="contentRoot">The content root path.</param>
/// <typeparam name="TStartup">Type of the startup type.</typeparam>
/// <remarks>When deriving this class and overriding <see cref="ConfigureWebHost"/> remember to call base.ConfigureWebHost.</remarks>
public abstract class ChronicleWebApplicationFactory<TStartup>(IChronicleSetupFixture fixture, ContentRoot contentRoot) : WebApplicationFactory<TStartup>
    where TStartup : class
{
    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder
            .UseContentRoot(contentRoot)
            .ConfigureServices(services =>
            {
                void OptionsConfigurator(ChronicleOptions options)
                {
                    options.ArtifactsProvider = fixture;
                    options.ConnectionString = "chronicle://localhost:35001?disableTls=true";
                }

                services.Configure<ChronicleAspNetCoreOptions>(OptionsConfigurator);
                services.Configure<ChronicleOptions>(OptionsConfigurator);
            });
    }
}
