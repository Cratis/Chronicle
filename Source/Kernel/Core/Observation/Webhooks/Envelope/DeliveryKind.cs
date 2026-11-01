// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Cratis.Chronicle.Observation.Webhooks.Envelope;

/// <summary>
/// Identifies the envelope data or reserved control kind. HTTP v1 is live-only.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<DeliveryKind>))]
public enum DeliveryKind
{
    /// <summary>
    /// Live events.
    /// </summary>
    [JsonStringEnumMemberName("events")]
    Events,

    /// <summary>
    /// Reserved replay boundary; not enabled in v1.
    /// </summary>
    [JsonStringEnumMemberName("replay-started")]
    ReplayStarted,

    /// <summary>
    /// Reserved replay boundary; not enabled in v1.
    /// </summary>
    [JsonStringEnumMemberName("replay-ended")]
    ReplayEnded,

    /// <summary>
    /// A control probe.
    /// </summary>
    [JsonStringEnumMemberName("ping")]
    Ping
}
