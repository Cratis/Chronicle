// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Represents what an idempotent append did for one event.
/// </summary>
public enum AppendOutcome
{
    /// <summary>
    /// The event was written by this call.
    /// </summary>
    Appended = 0,

    /// <summary>
    /// An event with the same key and the same content was already in the sequence; nothing was written.
    /// </summary>
    AlreadyAppended = 1,

    /// <summary>
    /// An event with the same key was already in the sequence but has since been erased; nothing was written.
    /// </summary>
    AlreadyAppendedAfterErasure = 2
}
