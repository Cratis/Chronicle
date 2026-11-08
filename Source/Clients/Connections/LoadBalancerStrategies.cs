// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography.X509Certificates;

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Factory for creating <see cref="ILoadBalancerStrategy"/> instances from their well-known names.
/// </summary>
public static class LoadBalancerStrategies
{
    /// <summary>
    /// Creates an <see cref="ILoadBalancerStrategy"/> from its well-known name.
    /// </summary>
    /// <param name="name">The name of the strategy. When not specified, defaults to least-connections.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation - used by strategies (e.g. least-connections) that probe the candidate servers themselves.</param>
    /// <param name="disableTls">Whether the servers are reached without TLS.</param>
    /// <returns>The <see cref="ILoadBalancerStrategy"/> for the name.</returns>
    /// <exception cref="UnknownLoadBalancerStrategy">Thrown when the name does not match a known strategy.</exception>
    public static ILoadBalancerStrategy Create(string? name = null, bool skipTlsValidation = false, bool disableTls = false) =>
        Create(name, skipTlsValidation, disableTls, certificateAuthorities: null);

    /// <summary>
    /// Creates an <see cref="ILoadBalancerStrategy"/> from its well-known name, validating server certificates against a set of certificate authorities.
    /// </summary>
    /// <param name="name">The name of the strategy. When <see langword="null"/> or empty, defaults to least-connections.</param>
    /// <param name="skipTlsValidation">Whether to skip TLS certificate validation - used by strategies (e.g. least-connections) that probe the candidate servers themselves.</param>
    /// <param name="disableTls">Whether the servers are reached without TLS.</param>
    /// <param name="certificateAuthorities">Certificate authorities the server certificates must chain to when validation is enabled - used by strategies that probe the candidate servers themselves; <see langword="null"/> uses the system trust store.</param>
    /// <returns>The <see cref="ILoadBalancerStrategy"/> for the name.</returns>
    /// <exception cref="UnknownLoadBalancerStrategy">Thrown when the name does not match a known strategy.</exception>
    public static ILoadBalancerStrategy Create(string? name, bool skipTlsValidation, bool disableTls, X509Certificate2Collection? certificateAuthorities) =>
        name switch
        {
            null or "" or LeastConnectionsLoadBalancerStrategy.StrategyName => new LeastConnectionsLoadBalancerStrategy(skipTlsValidation, certificateAuthorities, disableTls),
            RoundRobinLoadBalancerStrategy.StrategyName => new RoundRobinLoadBalancerStrategy(),
            RandomLoadBalancerStrategy.StrategyName => new RandomLoadBalancerStrategy(),
            _ => throw new UnknownLoadBalancerStrategy(name)
        };
}
