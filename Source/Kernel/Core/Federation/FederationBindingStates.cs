// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Federation;

/// <summary>
/// The well-known values of <see cref="FederationBindingStatus.State"/> written by the consumer.
/// </summary>
public static class FederationBindingStates
{
    /// <summary>
    /// The consumer is about to contact the producer.
    /// </summary>
    public const string Connecting = "Connecting";

    /// <summary>
    /// The consumer is connected and pulling.
    /// </summary>
    public const string Streaming = "Streaming";

    /// <summary>
    /// The link failed or was throttled and the consumer is waiting before retrying.
    /// </summary>
    public const string Backoff = "Backoff";

    /// <summary>
    /// The producer denied the link, for instance because it was revoked. The consumer retries rarely.
    /// </summary>
    public const string Denied = "Denied";

    /// <summary>
    /// The binding was removed or federation is disabled; nothing is pulled.
    /// </summary>
    public const string Disabled = "Disabled";

    /// <summary>
    /// The binding is healthy but no event store subscription asks for events from its source, so nothing is pulled.
    /// </summary>
    public const string Idle = "Idle";
}
