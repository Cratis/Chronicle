// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the phase of an event sequence mutation.
/// </summary>
public enum EventSequenceMutationPhase
{
    /// <summary>
    /// No mutation phase has been entered.
    /// </summary>
    None = 0,

    /// <summary>
    /// The mutation has been reserved.
    /// </summary>
    [SuppressMessage("Naming", "CA1700:Do not name enum values 'Reserved'", Justification = "The name and numeric value are fixed parts of the persisted storage contract.")]
    Reserved = 1,

    /// <summary>
    /// The mutation is being applied.
    /// </summary>
    Applying = 2,

    /// <summary>
    /// The mutation is being verified.
    /// </summary>
    Verifying = 3,

    /// <summary>
    /// The mutation is blocked.
    /// </summary>
    Blocked = 4,

    /// <summary>
    /// The mutation has been committed by the source.
    /// </summary>
    SourceCommitted = 5
}
