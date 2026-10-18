// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences;

/// <summary>
/// Describes how a keyed append was settled.
/// </summary>
public enum AppendOutcome
{
    /// <summary>
    /// This attempt committed the event.
    /// </summary>
    Appended = 0,

    /// <summary>
    /// The same intent was already durable.
    /// </summary>
    AlreadyAppended = 1,

    /// <summary>
    /// The knowable intent matched after protecting keys were erased.
    /// </summary>
    AlreadyAppendedAfterErasure = 2
}
