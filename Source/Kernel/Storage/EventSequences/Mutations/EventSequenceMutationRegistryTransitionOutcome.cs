// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the closed outcome of applying a registry mutation transition.
/// </summary>
public enum EventSequenceMutationRegistryTransitionOutcome
{
    /// <summary>No outcome was produced.</summary>
    Unknown = 0,

    /// <summary>The transition was applied.</summary>
    Applied = 1,

    /// <summary>The exact transition had already been applied.</summary>
    AlreadyApplied = 2,

    /// <summary>The mutation was already archived.</summary>
    AlreadyArchived = 3,

    /// <summary>The observed state does not match the supplied token or transition.</summary>
    StateConflict = 4,

    /// <summary>The operation lost a bounded contention race.</summary>
    Contended = 5,

    /// <summary>The provider cannot determine whether the operation took effect.</summary>
    Indeterminate = 6,

    /// <summary>The operation input is invalid.</summary>
    Invalid = 7,

    /// <summary>Persisted registry state is corrupt.</summary>
    Corrupt = 8,

    /// <summary>The provider does not support mutation registries.</summary>
    Unsupported = 9
}
