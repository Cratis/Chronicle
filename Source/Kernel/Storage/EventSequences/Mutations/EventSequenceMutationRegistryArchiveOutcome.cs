// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the closed outcome of archiving a registered event sequence mutation.
/// </summary>
public enum EventSequenceMutationRegistryArchiveOutcome
{
    /// <summary>No outcome was produced.</summary>
    Unknown = 0,

    /// <summary>The mutation was archived.</summary>
    Archived = 1,

    /// <summary>The exact mutation had already been archived.</summary>
    AlreadyArchived = 2,

    /// <summary>The observed state does not match the supplied token.</summary>
    StateConflict = 3,

    /// <summary>The operation lost a bounded contention race.</summary>
    Contended = 4,

    /// <summary>The provider cannot determine whether the operation took effect.</summary>
    Indeterminate = 5,

    /// <summary>The operation input is invalid.</summary>
    Invalid = 6,

    /// <summary>Persisted registry state is corrupt.</summary>
    Corrupt = 7,

    /// <summary>The provider does not support mutation registries.</summary>
    Unsupported = 8
}
