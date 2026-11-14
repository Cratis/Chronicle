// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Compliance.Redaction;

namespace Cratis.Chronicle.Diagnostics.Identifiers;

/// <summary>
/// Redacts classified identifiers according to the configured log exposure policy.
/// </summary>
/// <param name="identifiers">The identifier exposure policy.</param>
public sealed class IdentifierRedactor(IdentifierTelemetry identifiers) : Redactor
{
    const string Omitted = "[redacted]";

    /// <inheritdoc/>
    public override int GetRedactedLength(ReadOnlySpan<char> input) => identifiers.LogExposure switch
    {
        IdentifierExposure.Hashed => 32,
        IdentifierExposure.Raw => input.Length,
        _ => Omitted.Length
    };

    /// <inheritdoc/>
    public override int Redact(ReadOnlySpan<char> source, Span<char> destination)
    {
        var length = GetRedactedLength(source);
        if (destination.Length < length) throw new ArgumentException("Destination is too small.", nameof(destination));

        if (identifiers.LogExposure == IdentifierExposure.Hashed)
        {
            identifiers.TryHash(source, destination, out var written);
            return written;
        }

        if (identifiers.LogExposure == IdentifierExposure.Raw) source.CopyTo(destination);
        else Omitted.AsSpan().CopyTo(destination);
        return length;
    }
}
