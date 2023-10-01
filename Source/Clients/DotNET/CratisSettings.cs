// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Aksio.Cratis.Configuration;
using Aksio.Cratis.Identities;
using Aksio.Json;
using Aksio.Types;
using Microsoft.Extensions.Logging;

namespace Aksio.Cratis;

/// <summary>
/// Represents the settings for connecting to Cratis.
/// </summary>
public class CratisSettings
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CratisSettings"/> class.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to use.</param>
    /// <param name="kernelConnectivity"><see cref="KernelConnectivity"/> to use.</param>
    /// <param name="identityProvider">Optional <see cref="IIdentityProvider"/> to use. Will revert to default if not configured.</param>
    /// <param name="jsonSerializerOptions">Optional <see cref="JsonSerializerOptions"/> to use. Will revert to defaults if not configured.</param>
    /// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving instances of things like observers, reducers and other artifacts. Will revert to <see cref="DefaultServiceProvider"/> if not configured.</param>
    /// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. If not specified, it will use the <see cref="DefaultClientArtifactsProvider"/> with both project and package referenced assemblies.</param>
    /// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/> to use internally in client for logging.</param>
    public CratisSettings(
        CratisUrl url,
        KernelConnectivity kernelConnectivity,
        IIdentityProvider? identityProvider = null,
        JsonSerializerOptions? jsonSerializerOptions = null,
        IServiceProvider? serviceProvider = null,
        IClientArtifactsProvider? artifactsProvider = null,
        ILoggerFactory? loggerFactory = null)
    {
        Url = url;
        KernelConnectivity = kernelConnectivity;
        IdentityProvider = identityProvider ?? new BaseIdentityProvider();
        JsonSerializerOptions = jsonSerializerOptions ?? Globals.JsonSerializerOptions;
        LoggerFactory = loggerFactory ?? new LoggerFactory();
        ServiceProvider = serviceProvider ?? new DefaultServiceProvider();
        ArtifactsProvider = artifactsProvider ?? new DefaultClientArtifactsProvider(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));
    }

    /// <summary>
    /// Gets the <see cref="CratisUrl"/> to use.
    /// </summary>
    public CratisUrl Url { get; init; }

    /// <summary>
    /// Gets the <see cref="IIdentityProvider"/> to use.
    /// </summary>
    public IIdentityProvider IdentityProvider { get; init; }

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> to use.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; init; }

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to use.
    /// </summary>
    public IServiceProvider ServiceProvider { get; init; }

    /// <summary>
    /// Gets the <see cref="KernelConnectivity"/> to use.
    /// </summary>
    public KernelConnectivity KernelConnectivity { get; init; }

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> to use.
    /// </summary>
    public IClientArtifactsProvider ArtifactsProvider { get; init; }

    /// <summary>
    /// Gets the <see cref="ILoggerFactory"/> to use internally in the client.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; init; }

    /// <summary>
    /// Create a <see cref="CratisSettings"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">Connection string to create from.</param>
    /// <returns>A new <see cref="CratisSettings"/>.</returns>
    public static CratisSettings FromConnectionString(string connectionString) => FromUrl(new CratisUrl(connectionString));

    /// <summary>
    /// Create a <see cref="CratisSettings"/> from a <see cref="CratisUrl"/>.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to create from.</param>
    /// <returns>A new <see cref="CratisSettings"/>.</returns>
    public static CratisSettings FromUrl(CratisUrl url) =>
        new(url, new KernelConnectivity
        {
            SingleKernel = new SingleKernelOptions()
        });
}
