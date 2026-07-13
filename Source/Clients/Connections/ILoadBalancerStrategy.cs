// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Connections;

/// <summary>
/// Defines a strategy for selecting which Chronicle server to connect to when multiple are available.
/// </summary>
public interface ILoadBalancerStrategy
{
    /// <summary>
    /// Selects the next <see cref="ChronicleServerAddress"/> to connect to.
    /// </summary>
    /// <param name="serverAddresses">The available <see cref="ChronicleServerAddress"/> entries to select from.</param>
    /// <returns>The selected <see cref="ChronicleServerAddress"/>.</returns>
    /// <exception cref="MissingServerAddress">Thrown when there are no server addresses to select from.</exception>
    ChronicleServerAddress Next(IReadOnlyList<ChronicleServerAddress> serverAddresses);
}
