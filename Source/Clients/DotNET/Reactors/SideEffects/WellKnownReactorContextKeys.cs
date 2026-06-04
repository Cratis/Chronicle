// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Reactors.SideEffects;

/// <summary>
/// Well known keys for reactor context values.
/// </summary>
public static class WellKnownReactorContextKeys
{
    /// <summary>
    /// The key for the event source id in the reactor context values.
    /// </summary>
    public const string EventSourceId = "eventSourceId";

    /// <summary>
    /// The key for the event source type in the reactor context values.
    /// </summary>
    public const string EventSourceType = "eventSourceType";

    /// <summary>
    /// The key for the event stream type in the reactor context values.
    /// </summary>
    public const string EventStreamType = "eventStreamType";

    /// <summary>
    /// The key for the event stream id in the reactor context values.
    /// </summary>
    public const string EventStreamId = "eventStreamId";

    /// <summary>
    /// The key for the subject in the reactor context values.
    /// </summary>
    public const string Subject = "subject";
}
