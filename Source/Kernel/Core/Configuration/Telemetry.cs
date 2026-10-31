// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Controls event source id tracing in kernel telemetry.
/// </summary>
public class Telemetry
{
    /// <summary>
    /// Gets or inits how event source ids are included in spans. The default is Raw.
    /// </summary>
    public EventSourceIdTracing EventSourceIdTracing { get; init; } = EventSourceIdTracing.Raw;

    /// <summary>
    /// Gets or inits the deployment key used to hash event source ids. The key is never emitted in telemetry.
    /// </summary>
    [JsonIgnore]
    public string? EventSourceIdHashKey { get; init; }
}
