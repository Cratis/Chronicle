// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies the closed outcome of preparing a terminal mutation receipt.
/// </summary>
public enum EventSequenceMutationArchiveOutcome
{
    /// <summary>No archive outcome was produced.</summary>
    Unknown = 0,

    /// <summary>The terminal receipt was prepared.</summary>
    Prepared = 1,

    /// <summary>The current state or token is not eligible for this archive attempt.</summary>
    Conflict = 2,

    /// <summary>An input was malformed.</summary>
    Invalid = 3
}
