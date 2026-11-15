// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The reasons a pulled event is not appended to the inbox.
/// </summary>
public static class FederationSkipReasons
{
    /// <summary>
    /// The event originated in this event store; accepting it would create a loop.
    /// </summary>
    public const string OwnOrigin = "own-origin";

    /// <summary>
    /// The event was already forwarded.
    /// </summary>
    public const string Duplicate = "duplicate";

    /// <summary>
    /// No subscription wants the event.
    /// </summary>
    public const string Filtered = "filtered";
}
