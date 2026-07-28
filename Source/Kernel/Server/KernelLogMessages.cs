// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

internal static partial class KernelLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting Cratis Chronicle Server - Version {Version}")]
    internal static partial void ServerStarting(this ILogger<Kernel> logger, string version);

    [LoggerMessage(LogLevel.Information, "TLS certificate loaded successfully. Server will use HTTPS")]
    internal static partial void TlsCertificateLoaded(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Warning, "No TLS certificate configured. Generated a self-signed development certificate (Development mode only)")]
    internal static partial void DevelopmentCertificateGenerated(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Error, "No TLS certificate is configured. The Chronicle port requires a certificate to serve gRPC and HTTP on a single port")]
    internal static partial void TlsCertificateMissingProduction(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Debug, "Configuring server to listen on port {Port} for gRPC (HTTP/2) and Workbench, API and OAuth (HTTP/1.1)")]
    internal static partial void ServerListening(this ILogger<Kernel> logger, int port);

    [LoggerMessage(LogLevel.Information, "Exposing the health endpoint on dedicated port {Port} (TLS: {TlsEnabled})")]
    internal static partial void HealthEndpointListening(this ILogger<Kernel> logger, int port, bool tlsEnabled);

    [LoggerMessage(LogLevel.Debug, "Cratis Chronicle Server configured successfully - starting services")]
    internal static partial void ServerConfigured(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Information, "Cratis Chronicle Server started successfully - ready and listening on port {Port} for gRPC (HTTP/2) and Workbench, API and OAuth (HTTP/1.1)")]
    internal static partial void ServerStarted(this ILogger<Kernel> logger, int port);

    [LoggerMessage(LogLevel.Information, "Shutdown signal received. Chronicle Server is shutting down...")]
    internal static partial void ServerShuttingDown(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Warning, "The Workbench feature is enabled but the Workbench UI was not found - it is neither embedded in this build nor present in the web root '{WebRoot}' - the server runs without serving the Workbench UI")]
    internal static partial void WorkbenchUINotAvailable(this ILogger<Kernel> logger, string webRoot);

    [LoggerMessage(LogLevel.Warning, "Localhost clustering is configured while storage points at a non-local host. This node will form its own isolated single-node cluster - other nodes sharing this storage will each form a separate cluster over the same data, with no error at startup. Set Cratis__Chronicle__Clustering__Type=MongoDB on every node for them to join as one cluster")]
    internal static partial void LocalhostClusteringAgainstSharedStorage(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Critical, "Unhandled exception occurred (terminating: {IsTerminating})")]
    internal static partial void UnhandledException(this ILogger<Kernel> logger, Exception exception, bool isTerminating);

    [LoggerMessage(LogLevel.Error, "Unobserved task exception occurred")]
    internal static partial void UnobservedTaskException(this ILogger<Kernel> logger, Exception exception);
}
