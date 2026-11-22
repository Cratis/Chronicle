// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Installs and closes the namespace-local marker. Append admission is performed by event storage
/// in the same transaction/connection as its event inserts, never through this separate interface.
/// </summary>
public interface INamespaceMarkerStorage
{
    /// <summary>Reads the marker without creating one.</summary>
    /// <returns>The current marker, if installed.</returns>
    Task<NamespaceMarker?> Get();

    /// <summary>Installs a prepared identity, conditional on an absent or expected closed marker.</summary>
    /// <param name="incarnation">The prepared identity.</param>
    /// <param name="previous">The previous identity, or null for first initialization.</param>
    /// <returns>Whether the marker is open for exactly the prepared identity.</returns>
    Task<bool> TryOpen(NamespaceIncarnationId incarnation, NamespaceIncarnationId? previous);

    /// <summary>Closes the exact identity atomically with respect to admitted append transactions.</summary>
    /// <param name="incarnation">The identity to close.</param>
    /// <returns>Whether the marker was closed or was already closed for this identity.</returns>
    Task<bool> TryClose(NamespaceIncarnationId incarnation);
}
