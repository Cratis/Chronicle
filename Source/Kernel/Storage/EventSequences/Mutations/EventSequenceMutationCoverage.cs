// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the persisted mutation coverage of an event sequence.
/// </summary>
public enum EventSequenceMutationCoverage
{
    /// <summary>
    /// Mutation coverage is not tracked.
    /// </summary>
    Untracked = 0,

    /// <summary>
    /// Mutation coverage is tracked but not sealed.
    /// </summary>
    Unsealed = 1,

    /// <summary>
    /// Mutation coverage is sealed.
    /// </summary>
    Sealed = 2
}
