// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
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
    /// <summary>
    /// Gets a value indicating whether the derived factory wires its own <see cref="IChronicleClient"/>
    /// (and the <see cref="IEventStore"/>/<see cref="IChronicleConnection"/> family derived from it) -
    /// for example an in-process Orleans silo that builds the client directly against its own grain
    /// factory. When <see langword="true"/>, <see cref="ConfigureWebHost"/> skips its own
    /// <c>AddCratisChronicleClient()</c> registration so the two do not race: both register
    /// <see cref="IChronicleClient"/> as a singleton, and because DI resolves the last registration,
    /// whichever ran second would service resolution for the other's dependents - here that closed a
    /// cycle (this generic client resolving <see cref="IChronicleConnection"/> via the silo's
    /// <see cref="IEventStore"/> registration, which itself resolves <see cref="IChronicleClient"/> to
    /// build that same <see cref="IEventStore"/>) and hung every in-process fixture at startup.
    /// </summary>
    protected virtual bool RegistersOwnChronicleClient => false;

    /// <inheritdoc/>
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        var delegatingProvider = DelegatingClientArtifactsProvider.GetOrCreate(fixture);

        builder
            .UseContentRoot(contentRoot)
            .ConfigureServices(services =>
            {
                // Use delegating provider so the shared factory can serve artifacts
                // from whichever test fixture is currently active.
                services.AddSingleton<IClientArtifactsProvider>(delegatingProvider);

                if (RegistersOwnChronicleClient)
                {
                    return;
                }

                // AddCratisChronicleClient (the non-ASP.NET-Core-specific registration; this
                // factory hosts nothing of its own, it only holds services for
                // WebApplicationFactory) reads IOptions<ChronicleClientOptions> - a distinct DI
                // registration from IOptions<ChronicleOptions>, which nothing here consumes.
                //
                // The out-of-process server serves TLS with a self-signed test certificate,
                // so the client must skip certificate validation to connect (mirrors the
                // built-in Development connection string).
                services.Configure<ChronicleClientOptions>(options =>
                {
                    options.ConnectionString = "chronicle://localhost:35001?skipTlsValidation=true";
                    options.EventStore = Constants.EventStore;
                });

                services.AddCratisChronicleClient();
            });
    }
}
