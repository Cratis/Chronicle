// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the closed outcome of a pure mutation state transition.
/// </summary>
public enum EventSequenceMutationTransitionOutcome
{
    /// <summary>No transition outcome was produced.</summary>
    Unknown = 0,

    /// <summary>The successor was applied.</summary>
    Applied = 1,

    /// <summary>The exact successor had already been applied.</summary>
    AlreadyApplied = 2,

    /// <summary>The current valid state conflicts with the requested transition or token.</summary>
    Conflict = 3,

    /// <summary>An input was malformed and no mutation is returned.</summary>
    Invalid = 4
}
