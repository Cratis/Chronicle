// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the closed outcome of beginning an event sequence mutation.
/// </summary>
public enum EventSequenceMutationBeginOutcome
{
    /// <summary>No outcome was produced.</summary>
    Unknown = 0,

    /// <summary>A new mutation was permanently registered and reserved.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1700:Do not name enum values 'Reserved'", Justification = "Reserved is the required provider contract outcome for Chronicle#3920.")]
    Reserved = 1,

    /// <summary>The exact active mutation was found and resumed.</summary>
    Resumed = 2,

    /// <summary>A permanent registration was recovered into its active reservation.</summary>
    RecoveredReservation = 3,

    /// <summary>The exact mutation was already archived.</summary>
    Archived = 4,

    /// <summary>Another mutation is active for the target sequence.</summary>
    MutationAlreadyInProgress = 5,

    /// <summary>The mutation identifier is permanently bound to a different request.</summary>
    DefinitionConflict = 6,

    /// <summary>The operation lost a bounded contention race.</summary>
    Contended = 7,

    /// <summary>The provider cannot determine whether the operation took effect.</summary>
    Indeterminate = 8,

    /// <summary>The operation input is invalid.</summary>
    Invalid = 9,

    /// <summary>Persisted registry state is corrupt.</summary>
    Corrupt = 10,

    /// <summary>The provider does not support mutation registries.</summary>
    Unsupported = 11
}
