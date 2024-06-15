// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Identities;
using Cratis.Json;
using Cratis.Models;
using Cratis.Types;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the settings for connecting to Cratis.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CratisOptions"/> class.
/// </remarks>
/// <param name="url"><see cref="CratisUrl"/> to use.</param>
/// <param name="modelNameConvention">Optional <see cref="IModelNameConvention"/> to use.</param>
/// <param name="identityProvider">Optional <see cref="IIdentityProvider"/> to use. Will revert to default if not configured.</param>
/// <param name="jsonSerializerOptions">Optional <see cref="JsonSerializerOptions"/> to use. Will revert to defaults if not configured.</param>
/// <param name="serviceProvider">Optional <see cref="IServiceProvider"/> for resolving instances of things like observers, reducers and other artifacts. Will revert to <see cref="DefaultServiceProvider"/> if not configured.</param>
/// <param name="artifactsProvider">Optional <see cref="IClientArtifactsProvider"/>. If not specified, it will use the <see cref="DefaultClientArtifactsProvider"/> with both project and package referenced assemblies.</param>
/// <param name="connectTimeout">Optional timeout when connecting in seconds. Defaults to 5.</param>
/// <param name="loggerFactory">Optional <see cref="ILoggerFactory"/> to use internally in client for logging.</param>
public class CratisOptions(
    CratisUrl url,
    IModelNameConvention? modelNameConvention = null,
    IIdentityProvider? identityProvider = null,
    JsonSerializerOptions? jsonSerializerOptions = null,
    IServiceProvider? serviceProvider = null,
    IClientArtifactsProvider? artifactsProvider = null,
    int connectTimeout = 5,
    ILoggerFactory? loggerFactory = null)
{
    /// <summary>
    /// Gets the <see cref="CratisUrl"/> to use.
    /// </summary>
    public CratisUrl Url { get; init; } = url;

    /// <summary>
    /// Gets or sets the software version.
    /// </summary>
    public string SoftwareVersion { get; set; } = "0.0.0";

    /// <summary>
    /// Gets or sets the software commit.
    /// </summary>
    public string SoftwareCommit { get; set; } = "[N/A]";

    /// <summary>
    /// Gets or sets the program identifier.
    /// </summary>
    public string ProgramIdentifier { get; set; } = "[N/A]";

    /// <summary>
    /// Gets the <see cref="IIdentityProvider"/> to use.
    /// </summary>
    public IIdentityProvider IdentityProvider { get; init; } = identityProvider ?? new BaseIdentityProvider();

    /// <summary>
    /// Gets the <see cref="JsonSerializerOptions"/> to use.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; init; } = jsonSerializerOptions ?? Globals.JsonSerializerOptions;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> to use.
    /// </summary>
    public IServiceProvider ServiceProvider { get; init; } = serviceProvider ?? new DefaultServiceProvider();

    /// <summary>
    /// Gets the <see cref="IClientArtifactsProvider"/> to use.
    /// </summary>
    public IClientArtifactsProvider ArtifactsProvider { get; init; } = artifactsProvider ?? new DefaultClientArtifactsProvider(new CompositeAssemblyProvider(ProjectReferencedAssemblies.Instance, PackageReferencedAssemblies.Instance));

    /// <summary>
    /// Gets the <see cref="IModelNameConvention"/> to use.
    /// </summary>
    public IModelNameConvention ModelNameConvention { get; init; } = modelNameConvention ?? new DefaultModelNameConvention();

    /// <summary>
    /// Gets the timeout when connecting in seconds.
    /// </summary>
    public int ConnectTimeout { get; init; } = connectTimeout;

    /// <summary>
    /// Gets the <see cref="ILoggerFactory"/> to use internally in the client.
    /// </summary>
    public ILoggerFactory LoggerFactory { get; init; } = loggerFactory ?? new LoggerFactory();

    /// <summary>
    /// Create a <see cref="CratisOptions"/> from a connection string.
    /// </summary>
    /// <param name="connectionString">Connection string to create from.</param>
    /// <returns>A new <see cref="CratisOptions"/>.</returns>
    public static CratisOptions FromConnectionString(string connectionString) => FromUrl(new CratisUrl(connectionString));

    /// <summary>
    /// Create a <see cref="CratisOptions"/> from a <see cref="CratisUrl"/>.
    /// </summary>
    /// <param name="url"><see cref="CratisUrl"/> to create from.</param>
    /// <returns>A new <see cref="CratisOptions"/>.</returns>
    public static CratisOptions FromUrl(CratisUrl url) => new(url);
}
