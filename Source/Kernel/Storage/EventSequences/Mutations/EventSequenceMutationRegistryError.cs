// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies a non-sensitive typed registry error.
/// </summary>
public enum EventSequenceMutationRegistryError
{
    /// <summary>No registry error was produced.</summary>
    Unknown = 0,

    /// <summary>Another mutation is active for the target sequence.</summary>
    MutationAlreadyInProgress = 1,

    /// <summary>The mutation identifier is permanently bound to a different request.</summary>
    DefinitionConflict = 2,

    /// <summary>The observed mutation state conflicts with the requested operation.</summary>
    StateConflict = 3,

    /// <summary>The observed tracking coverage conflicts with the expected coverage.</summary>
    TrackingConflict = 4,

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
