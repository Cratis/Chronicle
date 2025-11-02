// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Applications.Commands;

/// <summary>
/// Well known keys for command context values.
/// </summary>
public static class WellKnownCommandContextKeys
{
    /// <summary>
    /// The key for the event source id in the command context values.
    /// </summary>
    public const string EventSourceId = "eventSourceId";

    /// <summary>
    /// The key for the event source type in the command context values.
    /// </summary>
    public const string EventSourceType = "eventSourceType";

    /// <summary>
    /// The key for the event stream type in the command context values.
    /// </summary>
    public const string EventStreamType = "eventStreamType";

    /// <summary>
    /// The key for the event stream id in the command context values.
    /// </summary>
    public const string EventStreamId = "eventStreamId";
}
