// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle;

/// <summary>
/// Controls Chronicle client traces.
/// </summary>
public class ChronicleTelemetryOptions
{
    /// <summary>
    /// Gets or sets the span naming convention. Defaults to the existing legacy operation and display names.
    /// </summary>
    public ClientSpanNames SpanNames { get; set; } = ClientSpanNames.Legacy;

    /// <summary>
    /// Gets or sets how event source ids are included in spans. The default is Raw.
    /// </summary>
    public EventSourceIdTracing EventSourceIdTracing { get; set; } = EventSourceIdTracing.Raw;

    /// <summary>
    /// Gets or sets the deployment key for Hashed tracing. Supply at least 32 characters from a secret store.
    /// </summary>
    [JsonIgnore]
    public string? EventSourceIdHashKey { get; set; }
}
