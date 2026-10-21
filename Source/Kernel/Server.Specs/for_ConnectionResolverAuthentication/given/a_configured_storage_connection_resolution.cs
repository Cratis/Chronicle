// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Chronicle.Server.for_ConnectionResolverAuthentication.given;

/// <summary>
/// Binds <see cref="ChronicleOptions"/> from configuration, the way the kernel does, and registers the storage
/// connection resolution together with the resolver authentication the kernel server adds.
/// </summary>
/// <remarks>
/// A recording access token provider, registered first, sees the authentication options the HTTP resolver is created
/// with. It never claims them, so the providers the kernel registers still decide who authenticates.
/// </remarks>
public class a_configured_storage_connection_resolution : Specification
{
    protected static readonly string Resolution = $"{ChronicleOptions.SectionPath}:Storage:ConnectionResolution:Connection";
    protected const string Endpoint = "https://platform-mgmt.example/internal/connections";

    protected IConnectionResolverAccessTokenProvider _recordingProvider;
    protected HttpConnectionResolverAuthenticationOptions? _authenticationReachingTheResolver;
    protected ChronicleOptions _options;
    protected IStorageConnectionResolution _resolution;
    protected Exception? _error;
    ServiceProvider? _serviceProvider;

    void Establish()
    {
        _recordingProvider = Substitute.For<IConnectionResolverAccessTokenProvider>();
        _recordingProvider
            .CanProvide(Arg.Do<HttpConnectionResolverAuthenticationOptions>(_ => _authenticationReachingTheResolver = _))
            .Returns(false);
    }

    void Destroy() => _serviceProvider?.Dispose();

    /// <summary>
    /// Bind the configuration and register the resolution the way the kernel server does.
    /// </summary>
    /// <param name="configuration">The configuration values.</param>
    /// <param name="withAzureAuthentication">Whether to register the Microsoft Entra ID authentication the kernel server adds.</param>
    protected void Configure(IDictionary<string, string?> configuration, bool withAzureAuthentication = true)
    {
        _options = new ConfigurationBuilder()
            .AddInMemoryCollection(configuration)
            .Build()
            .GetSection(ChronicleOptions.SectionPath)
            .Get<ChronicleOptions>() ?? new ChronicleOptions();

        var services = new ServiceCollection();
        services.AddSingleton(_recordingProvider);
        services.AddStorageConnectionResolution(WellKnownConnectionStorage.PostgreSql, _options.Storage.ConnectionResolution);
        if (withAzureAuthentication)
        {
            services.AddAzureConnectionResolverAuthentication();
        }

        _serviceProvider = services.BuildServiceProvider();
        _resolution = _serviceProvider.GetRequiredService<IStorageConnectionResolution>();
    }

    /// <summary>
    /// Create the resolver, capturing any error doing so.
    /// </summary>
    protected void CreateResolver() => _error = Catch.Exception(() => _ = _resolution.Resolver);
}
