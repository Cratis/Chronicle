// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Imports;

/// <summary>
/// Represents how one imported event settled when it was committed.
/// </summary>
public enum ImportEventOutcomeKind
{
    /// <summary>The event was appended.</summary>
    Appended = 0,

    /// <summary>The event's idempotency key was already durable, so it was not appended again.</summary>
    AlreadyAppended = 1,

    /// <summary>The event was not committed, for the reasons given.</summary>
    Rejected = 2
}
