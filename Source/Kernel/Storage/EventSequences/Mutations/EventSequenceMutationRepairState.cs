// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the repair state of an event sequence mutation.
/// </summary>
public enum EventSequenceMutationRepairState
{
    /// <summary>
    /// The repair state has not been specified.
    /// </summary>
    Unspecified = 0,

    /// <summary>
    /// No repair is required.
    /// </summary>
    NotRequired = 1,

    /// <summary>
    /// Repair is pending.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Repair is being dispatched.
    /// </summary>
    Dispatching = 3,

    /// <summary>
    /// Repair has been accepted.
    /// </summary>
    Accepted = 4,

    /// <summary>
    /// The repair state is unknown.
    /// </summary>
    Unknown = 5
}
