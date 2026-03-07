// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Server;

internal static partial class KernelLogMessages
{
    [LoggerMessage(LogLevel.Information, "Starting Cratis Chronicle Server - Version {Version}")]
    internal static partial void ServerStarting(this ILogger<Kernel> logger, string version);

    [LoggerMessage(LogLevel.Information, "TLS certificate loaded successfully. Server will use HTTPS")]
    internal static partial void TlsCertificateLoaded(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Warning, "No TLS certificate configured. Server will run without HTTPS (Development mode only)")]
    internal static partial void TlsCertificateMissingDevelopment(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Error, "No TLS certificate is configured. Server cannot start without HTTPS in non-development environments")]
    internal static partial void TlsCertificateMissingProduction(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Debug, "Configuring server to listen on management port {ManagementPort} (HTTP/1.1) and gRPC port {GrpcPort} (HTTP/2)")]
    internal static partial void ServerListening(this ILogger<Kernel> logger, int managementPort, int grpcPort);

    [LoggerMessage(LogLevel.Debug, "Cratis Chronicle Server configured successfully - starting services")]
    internal static partial void ServerConfigured(this ILogger<Kernel> logger);

    [LoggerMessage(LogLevel.Information, "Cratis Chronicle Server started successfully - ready and listening on management port {ManagementPort} (HTTP/1.1) and gRPC port {GrpcPort} (HTTP/2)")]
    internal static partial void ServerStarted(this ILogger<Kernel> logger, int managementPort, int grpcPort);

    [LoggerMessage(LogLevel.Information, "Shutdown signal received. Chronicle Server is shutting down...")]
    internal static partial void ServerShuttingDown(this ILogger<Kernel> logger);
}
