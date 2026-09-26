// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Transactions;

/// <summary>An opaque completion capability owned by the transaction orchestrator.</summary>
public sealed class DecisionReadCommitOwner
{
    /// <summary>Creates a capability for the owning unit of work.</summary>
    internal DecisionReadCommitOwner()
    {
    }
}
