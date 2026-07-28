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
    /// <returns>A <see cref="Task{TResult}"/> that resolves to the selected <see cref="ChronicleServerAddress"/>.</returns>
    /// <exception cref="MissingServerAddress">Thrown when there are no server addresses to select from.</exception>
    /// <remarks>
    /// Async because a strategy may need to reach out to the candidate servers themselves (e.g. to
    /// ask each one how many connections it currently holds) before it can decide.
    /// </remarks>
    Task<ChronicleServerAddress> Next(IReadOnlyList<ChronicleServerAddress> serverAddresses);
}
