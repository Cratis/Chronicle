// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;
using Cratis.Monads;

namespace Cratis.Chronicle.EventSequences.Idempotency;

/// <summary>
/// Computes an append intent without reading or provisioning encryption keys.
/// </summary>
internal static class AppendFingerprint
{
    /// <summary>
    /// Locates boundaries using compliance's traversal and maps its unsupported-boundary exception to a normal refusal.
    /// </summary>
    /// <param name="metadata">The compliance metadata manager.</param>
    /// <param name="schema">The requested event schema.</param>
    /// <param name="subject">The effective subject.</param>
    /// <param name="content">The plaintext content.</param>
    /// <returns>The locations, or an unsupported refusal.</returns>
    internal static async Task<Result<IReadOnlyList<ProtectedValueLocation>, FingerprintFailure>> LocateProtectedValues(
        IJsonSchemaMetadataManager metadata,
        JsonSchema schema,
        string subject,
        JsonObject content)
    {
        try
        {
            return Result<IReadOnlyList<ProtectedValueLocation>, FingerprintFailure>.Success(await metadata.LocateProtectedValues(schema, subject, content));
        }
        catch (ProtectedBoundaryUnsupported)
        {
            return FingerprintFailure.FingerprintUnsupported;
        }
    }

    /// <summary>
    /// Computes the intent using only the keys reported by encryption.
    /// </summary>
    /// <param name="input">The plaintext intent.</param>
    /// <param name="uses">The encryption reports.</param>
    /// <returns>The fingerprint or a refusal.</returns>
    internal static Result<StoredAppendFingerprint, FingerprintFailure> Compute(AppendFingerprintInput input, IReadOnlyList<ProtectedValueUse> uses)
    {
        var plain = Plain(input);
        if (!plain.TryGetResult(out var hash) || !ReportsMatch(input.ProtectedValues, uses))
        {
            return FingerprintFailure.FingerprintUnsupported;
        }

        var parts = new List<ProtectedFingerprintPart>();
        foreach (var group in uses.GroupBy(use => use.Kind).OrderBy(group => group.Key))
        {
            var key = group.First().Key;
            if (group.Any(use => use.Key.KeyReference != key.KeyReference || use.Key.Revision != key.Revision))
            {
                return FingerprintFailure.FingerprintKeysDiverged;
            }

            if (group.Any(use => use.Key.Identifier != key.Identifier))
            {
                return FingerprintFailure.FingerprintUnsupported;
            }

            var values = group.Select(use => new ProtectedValueLocation(use.Path, use.Kind, use.Key.Identifier, use.Plaintext));
            parts.Add(new(group.Key, key.KeyReference, key.Revision, ProtectedHash(input.Identity, key.Key, values)));
        }

        return new StoredAppendFingerprint(hash, input.EventType, parts.ToArray());
    }

    /// <summary>
    /// Computes the plain digest without accessing any key.
    /// </summary>
    /// <param name="input">The plaintext intent.</param>
    /// <returns>The digest, or an unsupported-boundary refusal.</returns>
    internal static Result<string, FingerprintFailure> Plain(AppendFingerprintInput input)
    {
        var masked = FingerprintContent.Mask(input);
        if (masked is null)
        {
            return FingerprintFailure.FingerprintUnsupported;
        }

        string[] fields = [input.EventType.Id.Value, input.EventType.Generation.Value.ToString(CultureInfo.InvariantCulture),
            CanonicalJson.Encode(masked), input.EventSourceId.Value, input.EventSourceType.Value, input.EventStreamType.Value,
            input.EventStreamId.Value, input.EventSource?.Value ?? string.Empty, input.Subject];
        var boundaries = input.ProtectedValues.OrderBy(value => value.Path, StringComparer.Ordinal)
            .SelectMany(value => new[] { value.Path, FingerprintEncoding.Name(value.Kind)! });
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(FingerprintEncoding.Fields(fields.Concat(boundaries)))));
    }

    /// <summary>
    /// Computes the v1 HMAC using a short-lived derived key, never persisting secret derivatives.
    /// </summary>
    /// <param name="identity">The append identity.</param>
    /// <param name="key">The exact encrypting key.</param>
    /// <param name="values">The protected plaintext boundaries.</param>
    /// <returns>The lowercase HMAC digest.</returns>
    internal static string ProtectedHash(string identity, EncryptionKey key, IEnumerable<ProtectedValueLocation> values)
    {
        var derived = HKDF.DeriveKey(HashAlgorithmName.SHA256, key.Private, 32, Encoding.UTF8.GetBytes(identity), "cratis-chronicle/append-fingerprint/v1"u8.ToArray());
        try
        {
            var fields = values.OrderBy(value => value.Path, StringComparer.Ordinal).SelectMany(value => new[] { value.Path, CanonicalJson.Encode(value.Plaintext) });
            return Convert.ToHexStringLower(HMACSHA256.HashData(derived, Encoding.UTF8.GetBytes(FingerprintEncoding.Fields(fields))));
        }
        finally
        {
            CryptographicOperations.ZeroMemory(derived);
        }
    }

    static bool ReportsMatch(IReadOnlyList<ProtectedValueLocation> locations, IReadOnlyList<ProtectedValueUse> uses) =>
        locations.Count == uses.Count && uses.Select(use => use.Path).Distinct(StringComparer.Ordinal).Count() == uses.Count &&
        locations.All(location => uses.Any(use => use.Path == location.Path && use.Kind == location.Kind && use.Key.Kind == location.Kind &&
            use.Key.Identifier == location.Identifier && use.Key.Revision.Value > 0 && use.Key.Revision.Value < uint.MaxValue &&
            CanonicalJson.Encode(use.Plaintext) == CanonicalJson.Encode(location.Plaintext)));
}
