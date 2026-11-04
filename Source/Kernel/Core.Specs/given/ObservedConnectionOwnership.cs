// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Clients;
using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.given;

/// <summary>
/// Exposes an entry barrier without changing the real lifecycle gate.
/// </summary>
public class ObservedConnectionOwnership : ConnectionOwnership, IConnectionOwnership
{
    /// <summary>
    /// Gets or sets the action invoked immediately before a service waits for its gate.
    /// </summary>
    public Action? Entering { get; set; }

    ValueTask<IDisposable> IConnectionOwnership.Enter(ConnectionId id, CancellationToken cancellationToken)
    {
        Entering?.Invoke();
        return Enter(id, cancellationToken);
    }
}
