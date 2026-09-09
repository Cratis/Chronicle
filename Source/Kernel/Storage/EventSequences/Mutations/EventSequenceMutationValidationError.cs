// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies why an event sequence mutation value is invalid.
/// </summary>
public enum EventSequenceMutationValidationError
{
    /// <summary>The value is valid.</summary>
    None = 0,

    /// <summary>A required value is missing.</summary>
    MissingValue = 1,

    /// <summary>The event sequence scope is not initialized.</summary>
    InvalidScope = 2,

    /// <summary>An identity is unsupported or does not match its canonical key.</summary>
    InvalidIdentity = 3,

    /// <summary>An identifier is a sentinel rather than an actual value.</summary>
    InvalidId = 4,

    /// <summary>An ordinal is not positive.</summary>
    InvalidOrdinal = 5,

    /// <summary>A state version is not positive.</summary>
    InvalidStateVersion = 6,

    /// <summary>An enum value is undefined or not valid in its context.</summary>
    InvalidEnum = 7,

    /// <summary>A command envelope is malformed.</summary>
    InvalidCommand = 8,

    /// <summary>A target range or count is malformed.</summary>
    InvalidTarget = 9,

    /// <summary>A digest is missing or does not match recomputation.</summary>
    InvalidDigest = 10,

    /// <summary>The active phase, blocked phase, and repair state do not form a valid composite.</summary>
    InvalidComposite = 11,

    /// <summary>The registration lifecycle fields do not form a valid matrix row.</summary>
    InvalidRegistration = 12,

    /// <summary>A terminal history entry or witness is malformed.</summary>
    InvalidTerminal = 13,

    /// <summary>The requested state transition would exhaust the state version.</summary>
    StateVersionExhausted = 14,

    /// <summary>The mutation is not eligible for archival.</summary>
    NotArchiveEligible = 15
}
