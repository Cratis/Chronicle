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
}
