// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;
using Cratis.Chronicle.Diagnostics;

namespace Cratis.Chronicle.Configuration;

/// <summary>
/// Represents the telemetry configuration.
/// </summary>
public class Telemetry
{
    /// <summary>
    /// Gets or inits the metric naming mode. Legacy is the default until the next major release;
    /// Convention opts into the shared names, and Both publishes both sets during migration.
    /// </summary>
    public MetricNames MetricNames { get; init; } = MetricNames.Legacy;

    /// <summary>
    /// Gets or inits whether Orleans runtime and grain-call spans are exported. Defaults to false:
    /// these sources accounted for about 99% of kernel spans. Trace context propagation stays enabled.
    /// </summary>
    public bool OrleansTracing { get; init; }

    EventSourceIdTracing? _eventSourceIdTracing;

    /// <summary>
    /// Gets or inits how event source ids are included in spans. The default is Hashed. When no
    /// <see cref="EventSourceIdHashKey"/> is configured, a random key is generated for the lifetime of the process,
    /// so hashes only correlate within one process.
    /// </summary>
    public EventSourceIdTracing EventSourceIdTracing
    {
        get => _eventSourceIdTracing ?? EventSourceIdTracing.Hashed;
        init => _eventSourceIdTracing = value;
    }

    /// <summary>
    /// Gets whether a hash key must be valid: hashing was chosen explicitly, or a key was supplied.
    /// The implicit default of hashing without any key uses a generated key and needs none.
    /// </summary>
    internal bool RequiresHashKey =>
        EventSourceIdTracing == EventSourceIdTracing.Hashed &&
        (_eventSourceIdTracing.HasValue || !string.IsNullOrEmpty(EventSourceIdHashKey));

    /// <summary>
    /// Gets or inits the deployment key used to hash event source ids. The key is never emitted in telemetry.
    /// </summary>
    [JsonIgnore]
    public string? EventSourceIdHashKey { get; init; }

    /// <summary>
    /// Gets whether gRPC client tracing is enabled. Defaults to false.
    /// </summary>
    /// <remarks>
    /// Client spans for every kernel-to-kernel or outbound gRPC call account for a large share
    /// of trace volume, so this instrumentation is opt-in.
    /// </remarks>
    public bool GrpcClientTracing { get; init; }
}
