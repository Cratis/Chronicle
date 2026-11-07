// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias KernelCore;
extern alias KernelConcepts;

using KernelClaim = KernelCore::Cratis.Chronicle.Clients.ConnectionClaim;
using KernelConnectionId = KernelConcepts::Cratis.Chronicle.Concepts.Clients.ConnectionId;
using KernelConnectionOwner = KernelCore::Cratis.Chronicle.Clients.ConnectionOwner;

namespace Cratis.Chronicle.Testing;

/// <summary>
/// Represents an in-process <see cref="KernelCore::Cratis.Chronicle.Clients.IConnectionOwnership"/> where every connection belongs to the single in-process caller.
/// </summary>
internal sealed class InProcessConnectionOwnership : KernelCore::Cratis.Chronicle.Clients.IConnectionOwnership
{
    /// <inheritdoc/>
    public KernelClaim Claim(KernelConnectionId connectionId, KernelConnectionOwner owner) => KernelClaim.Claimed;

    /// <inheritdoc/>
    public void Release(KernelConnectionId connectionId, KernelConnectionOwner owner)
    {
    }

    /// <inheritdoc/>
    public bool IsOwnedBy(string connectionId, KernelConnectionOwner owner) => true;
}
