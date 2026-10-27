// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Applies the configured event source id tracing policy.
/// </summary>
public sealed class IdentifierTelemetry
{
    readonly IdentifierHasher? _hasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="IdentifierTelemetry"/> class.
    /// </summary>
    /// <param name="telemetry">The telemetry configuration.</param>
    public IdentifierTelemetry(Telemetry telemetry)
    {
        Tracing = telemetry.EventSourceIdTracing;
        if (Tracing == EventSourceIdTracing.Hashed)
        {
            _hasher = new IdentifierHasher(telemetry.EventSourceIdHashKey ?? string.Empty);
        }
    }

    /// <summary>
    /// Gets the configured event source id tracing mode.
    /// </summary>
    public EventSourceIdTracing Tracing { get; }

    /// <summary>
    /// Get the permitted representation of an event source id in a span.
    /// </summary>
    /// <param name="id">The event source id.</param>
    /// <returns>The permitted representation, or null when event source ids are omitted.</returns>
    public string? ForSpan(string id) => Tracing switch
    {
        EventSourceIdTracing.Hashed => _hasher!.Hash(id),
        EventSourceIdTracing.Raw => id,
        _ => null
    };
}
