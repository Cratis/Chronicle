// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the closed outcome of beginning mutation tracking for an event sequence.
/// </summary>
public enum EventSequenceMutationTrackingOutcome
{
    /// <summary>No outcome was produced.</summary>
    Unknown = 0,

    /// <summary>Tracking began and the head is unsealed.</summary>
    Began = 1,

    /// <summary>Tracking had already begun and the head is unsealed.</summary>
    AlreadyTracking = 2,

    /// <summary>The observed coverage conflicts with the expected coverage.</summary>
    Conflict = 3,

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
