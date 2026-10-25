// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Diagnostics;

/// <summary>
/// Applies event source id tracing before an id reaches an activity.
/// </summary>
internal sealed class EventSourceIdTagging
{
    readonly EventSourceIdTracing _tracing;
    readonly IdentifierHasher? _hasher;

    /// <summary>
    /// Initializes a new instance of the <see cref="EventSourceIdTagging"/> class.
    /// </summary>
    /// <param name="options">The client telemetry configuration.</param>
    /// <exception cref="InvalidIdentifierHashKey">Hashed tracing requires a key of at least 32 characters.</exception>
    public EventSourceIdTagging(ChronicleTelemetryOptions options)
    {
        _tracing = options.EventSourceIdTracing;
        if (_tracing == EventSourceIdTracing.Hashed)
        {
            if (string.IsNullOrEmpty(options.EventSourceIdHashKey) || options.EventSourceIdHashKey.Length < IdentifierHasher.MinimumKeyLength)
            {
                throw new InvalidIdentifierHashKey();
            }

            _hasher = new IdentifierHasher(options.EventSourceIdHashKey);
        }
    }

    /// <summary>
    /// Gets the representation permitted in a span.
    /// </summary>
    /// <param name="id">The original event source id.</param>
    /// <returns>The unchanged id, a keyed hash, or null when omitted.</returns>
    public string? ForSpan(string id) => _tracing switch
    {
        EventSourceIdTracing.Raw => id,
        EventSourceIdTracing.Hashed => _hasher!.Hash(id),
        _ => null
    };
}
