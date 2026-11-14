// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Applies the configured identifier exposure policy to spans and logs.
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
        SpanExposure = telemetry.EventSourceIdsInSpans;
        LogExposure = telemetry.EventSourceIdsInLogs;
        if (SpanExposure == IdentifierExposure.Hashed || LogExposure == IdentifierExposure.Hashed)
        {
            _hasher = new IdentifierHasher(telemetry.IdentifierHashKey ?? string.Empty);
        }
    }

    /// <summary>
    /// Gets the configured identifier exposure in spans.
    /// </summary>
    public IdentifierExposure SpanExposure { get; }

    /// <summary>
    /// Gets the configured identifier exposure in logs.
    /// </summary>
    public IdentifierExposure LogExposure { get; }

    /// <summary>
    /// Get the permitted representation of an identifier in a span.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The permitted representation, or null when identifiers are omitted.</returns>
    public string? ForSpan(string id) => ForExposure(id, SpanExposure);

    /// <summary>
    /// Get the permitted representation of an identifier in a log.
    /// </summary>
    /// <param name="id">The identifier.</param>
    /// <returns>The permitted representation, or null when identifiers are omitted.</returns>
    public string? ForLog(string id) => ForExposure(id, LogExposure);

    /// <summary>
    /// Try to hash an identifier into a destination span with the configured key.
    /// </summary>
    /// <param name="value">The identifier.</param>
    /// <param name="destination">The destination for the lowercase hexadecimal hash.</param>
    /// <param name="written">The number of characters written.</param>
    /// <returns>Whether the destination was large enough for the hash.</returns>
    internal bool TryHash(ReadOnlySpan<char> value, Span<char> destination, out int written) => _hasher!.TryHash(value, destination, out written);

    string? ForExposure(string id, IdentifierExposure exposure) => exposure switch
    {
        IdentifierExposure.Hashed => _hasher!.Hash(id),
        IdentifierExposure.Raw => id,
        _ => null
    };
}
