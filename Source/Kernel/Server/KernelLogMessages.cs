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

    [LoggerMessage(LogLevel.Warning, "TLS is disabled - serving cleartext gRPC (h2c) on port {GrpcPort} and Workbench, API, OAuth and health (HTTP/1.1) on port {ManagementPort}")]
    internal static partial void TlsDisabled(this ILogger<Kernel> logger, int grpcPort, int managementPort);

    [LoggerMessage(LogLevel.Information, "Serving the health check endpoint on dedicated plaintext port {HealthPort}")]
    internal static partial void HealthPortEnabled(this ILogger<Kernel> logger, int healthPort);

    [LoggerMessage(LogLevel.Debug, "Configuring server to listen on port {Port}")]
    internal static partial void ServerListening(this ILogger<Kernel> logger, int port);

    [LoggerMessage(LogLevel.Debug, "Cratis Chronicle Server configured successfully - starting services")]
    internal static partial void ServerConfigured(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Information, "Cratis Chronicle Server started successfully - ready and listening on port {Port}")]
    internal static partial void ServerStarted(this ILogger<Kernel> logger, int port);

    [LoggerMessage(LogLevel.Information, "Shutdown signal received. Chronicle Server is shutting down...")]
    internal static partial void ServerShuttingDown(this ILogger<Kernel> logger);
}
