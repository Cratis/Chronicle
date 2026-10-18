// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// The non-secret persisted parts of an append intent. Its v1 text is a persistent format, pinned by golden specifications.
/// </summary>
/// <param name="Plain">The lowercase plain digest.</param>
/// <param name="EventType">The original type and generation.</param>
/// <param name="ProtectedParts">The keyed digests.</param>
internal record StoredAppendFingerprint(string Plain, EventType EventType, IReadOnlyList<ProtectedFingerprintPart> ProtectedParts)
{
    /// <summary>
    /// Formats the persistent v1 representation.
    /// </summary>
    /// <returns>The stored text.</returns>
    internal string Format() => $"v1:{Plain};type={FingerprintEncoding.Fields([EventType.Id.Value, EventType.Generation.Value.ToString(CultureInfo.InvariantCulture)])}" +
        string.Concat(ProtectedParts.OrderBy(part => part.Kind).Select(part =>
            $";{FingerprintEncoding.Name(part.Kind)}:{part.KeyReference}@{part.Revision.Value.ToString(CultureInfo.InvariantCulture)}={part.Hash}"));

    /// <summary>
    /// Parses v1 without interpreting delimiters embedded in length-prefixed type ids.
    /// </summary>
    /// <param name="text">The persistent text.</param>
    /// <returns>The fingerprint, or an unknown-version or malformed-format refusal.</returns>
    internal static Result<StoredAppendFingerprint, FingerprintParseError> Parse(string text)
    {
        var versionEnd = text.IndexOf(':', StringComparison.Ordinal);

        // A syntactically valid version other than v1 is different from damaged text.
        if (versionEnd >= 2 && text[0] == 'v' && text.AsSpan(1, versionEnd - 1).IndexOfAnyExceptInRange('0', '9') < 0 && text[..versionEnd] != "v1")
        {
            return FingerprintParseError.UnknownFingerprintVersion;
        }

        if (!text.StartsWith("v1:", StringComparison.Ordinal) || text.Length < 73 ||
            !FingerprintEncoding.IsDigest(text[3..67]) || text[67..73] != ";type=")
        {
            return FingerprintParseError.MalformedFingerprint;
        }

        var position = 73;
        if (!TryReadField(text, ref position, out var typeId) || !TryReadField(text, ref position, out var generationText) ||
            !TryReadNumber(generationText, out var generation))
        {
            return FingerprintParseError.MalformedFingerprint;
        }

        var parts = new List<ProtectedFingerprintPart>();
        while (position < text.Length)
        {
            if (text[position++] != ';')
            {
                return FingerprintParseError.MalformedFingerprint;
            }

            var end = text.IndexOf(';', position);
            end = end < 0 ? text.Length : end;
            var part = ParsePart(text[position..end]);
            if (part is null || (parts.Count > 0 && part.Kind <= parts[^1].Kind))
            {
                return FingerprintParseError.MalformedFingerprint;
            }

            parts.Add(part);
            position = end;
        }

        return new StoredAppendFingerprint(text[3..67], new(typeId, generation), parts.ToArray());
    }

    static ProtectedFingerprintPart? ParsePart(string text)
    {
        var colon = text.IndexOf(':', StringComparison.Ordinal);
        var at = text.IndexOf('@', StringComparison.Ordinal);
        var equals = text.IndexOf('=', StringComparison.Ordinal);
        if (colon < 1 || at <= colon || equals <= at)
        {
            return null;
        }

        var kind = Enum.GetValues<ProtectionKind>().Where(kind => FingerprintEncoding.Name(kind) == text[..colon]).Cast<ProtectionKind?>().FirstOrDefault();
        var reference = text[(colon + 1)..at];
        var hash = text[(equals + 1)..];
        return kind.HasValue && FingerprintEncoding.IsDigest(reference) && FingerprintEncoding.IsDigest(hash) &&
            TryReadNumber(text[(at + 1)..equals], out var revision) && revision > 0 && revision < uint.MaxValue
            ? new(kind.Value, reference, revision, hash)
            : null;
    }

    static bool TryReadNumber(string text, out uint number) =>
        uint.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number.ToString(CultureInfo.InvariantCulture) == text;

    static bool TryReadField(string text, ref int position, out string value)
    {
        value = string.Empty;
        var colon = text.IndexOf(':', position);
        if (colon < 0 || !TryReadNumber(text[position..colon], out var length) || length >= (uint)(text.Length - colon - 1))
        {
            return false;
        }

        var end = colon + 1 + (int)length;
        if (text[end] != '|')
        {
            return false;
        }

        value = text[(colon + 1)..end];
        position = end + 1;
        return true;
    }
}
