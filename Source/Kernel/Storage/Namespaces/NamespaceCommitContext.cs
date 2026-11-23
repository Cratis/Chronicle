// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Binds a protected event commit to the incarnation captured by its calling activation.
/// </summary>
/// <param name="Incarnation">The expected identity; providers must never replace it with a refreshed identity.</param>
/// <param name="ActivationCancellation">Cancellation raised when the calling activation can no longer commit.</param>
public sealed record NamespaceCommitContext(NamespaceIncarnationId Incarnation, CancellationToken ActivationCancellation)
{
    /// <summary>
    /// Gets the incarnation captured when the calling activation loaded its state. Null means unknown.
    /// Providers compare this only after acquiring the expected incarnation's marker fence.
    /// </summary>
    public NamespaceIncarnationId? ActivationIncarnation { get; init; }
}
