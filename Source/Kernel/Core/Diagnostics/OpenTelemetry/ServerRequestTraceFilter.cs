// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Frozen;
using Cratis.Chronicle.Compatibility;
using Cratis.Chronicle.Configuration;
using Cratis.Chronicle.Contracts;
using Cratis.Chronicle.Contracts.Clients;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry;

/// <summary>
/// Decides which server requests are traced; health checks, the connection keep-alive and long-lived gRPC streams are not.
/// </summary>
internal static class ServerRequestTraceFilter
{
    static readonly FrozenSet<string> _untracedGrpcPaths = GetUntracedGrpcPaths();

    /// <summary>
    /// Determines whether a request to the given path should produce a server span.
    /// </summary>
    /// <param name="healthCheckEndpoint">The configured health check endpoint, if any.</param>
    /// <param name="path">The request path.</param>
    /// <returns>True if the request should be traced; otherwise false.</returns>
    public static bool ShouldTrace(string? healthCheckEndpoint, string? path) =>
        !HealthOnlyPortPolicy.IsHealthCheckEndpoint(healthCheckEndpoint, path) &&
        (path is null || !_untracedGrpcPaths.Contains(path));

    static FrozenSet<string> GetUntracedGrpcPaths()
    {
        var services = WireContractReader.Read(WireContractDescriptorSet.Bytes).Services.Values;
        var connection = services.SingleOrDefault(_ => _.FullName == ".Cratis.Chronicle.Contracts.Clients.ConnectionService");
        if (connection?.Methods.ContainsKey(nameof(IConnectionService.ConnectionKeepAlive)) != true)
        {
            throw new MissingKeepAliveOperation();
        }

        return services.SelectMany(service => service.Methods.Values
                .Where(method => method.ClientStreaming || method.ServerStreaming)
                .Select(method => $"/{service.FullName.TrimStart('.')}/{method.Name}"))
            .Append($"/{connection.FullName.TrimStart('.')}/{nameof(IConnectionService.ConnectionKeepAlive)}")
            .ToFrozenSet(StringComparer.Ordinal);
    }
}
