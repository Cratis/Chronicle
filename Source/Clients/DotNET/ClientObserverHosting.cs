// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;

namespace Cratis.Chronicle;

/// <summary>
/// Helpers for deciding whether a connection can host client-side observers.
/// </summary>
internal static class ClientObserverHosting
{
    /// <summary>
    /// Gets whether the connection can host client-side observers.
    /// </summary>
    /// <param name="connection">The <see cref="IChronicleConnection"/> to check.</param>
    /// <returns>True if observers can be hosted, false if the connection skips keep-alive.</returns>
    internal static bool IsAvailable(IChronicleConnection? connection) => connection is not IConnectionKeepAlive { IsSkipped: true };

    /// <summary>
    /// Throws if the connection cannot host client-side observers.
    /// </summary>
    /// <param name="connection">The <see cref="IChronicleConnection"/> to check.</param>
    /// <param name="observerKind">The kind of observer being registered.</param>
    /// <param name="observerId">Provides the identifier of the observer being registered; only evaluated when throwing.</param>
    /// <exception cref="ClientObserversRequireKeepAlive">Thrown when the connection skips keep-alive.</exception>
    internal static void ThrowIfUnavailable(IChronicleConnection? connection, string observerKind, Func<string> observerId)
    {
        if (!IsAvailable(connection))
        {
            throw new ClientObserversRequireKeepAlive(observerKind, observerId());
        }
    }
}
