// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the permanent lifecycle of a mutation registration.
/// </summary>
public enum EventSequenceMutationRegistryLifecycle
{
    /// <summary>
    /// The lifecycle is unknown.
    /// </summary>
    Unknown = 0,

    /// <summary>
    /// The request has permanently claimed its identifier.
    /// </summary>
    Claimed = 1,

    /// <summary>
    /// The request has been assigned an ordinal.
    /// </summary>
    Bound = 2,

    /// <summary>
    /// The request has been archived with a terminal witness.
    /// </summary>
    Archived = 3
}
