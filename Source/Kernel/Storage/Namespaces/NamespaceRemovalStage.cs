// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Namespaces;

/// <summary>
/// Ordered checkpoints of namespace removal. A checkpoint advances only after its effect is acknowledged.
/// </summary>
public enum NamespaceRemovalStage
{
    /// <summary>No removal has begun.</summary>
    None = 0,

    /// <summary>Close the namespace-local marker and drain admitted appends.</summary>
    CloseMarker = 1,

    /// <summary>Forget the active namespace listing.</summary>
    Forget = 2,

    /// <summary>Perform bounded, idempotent observer cleanup.</summary>
    Cleanup = 3,

    /// <summary>Drop the namespace database and await a terminal result.</summary>
    Drop = 4,

    /// <summary>Remove the durable namespace listing.</summary>
    DeleteListing = 5,

    /// <summary>All removal effects have been acknowledged.</summary>
    Complete = 6
}
