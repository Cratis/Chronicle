// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.AzureKeyVault;

/// <summary>
/// What an erasure record says about the keys of an address. A record that cannot be read covers every key.
/// </summary>
/// <param name="Readable">Whether the record could be read.</param>
/// <param name="ErasedThrough">The highest revision the erasure covers.</param>
/// <param name="ErasedKeyFingerprints">The fingerprints of the keys the erasure covers.</param>
internal record LegacyErasureFence(bool Readable, uint ErasedThrough, IReadOnlyList<string> ErasedKeyFingerprints)
{
    const string ErasedThroughField = "erasedThrough";
    const string ErasedKeyFingerprintsField = "erasedKeyFingerprints";
    const string PublicKeyField = "publicKey";

    /// <summary>
    /// Reads a fence from the value of an erasure record.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The <see cref="LegacyErasureFence"/>; unreadable when the value is not an erasure record.</returns>
    public static LegacyErasureFence Parse(string value)
    {
        var data = Deserialize(value);
        if (data is null ||
            !data.TryGetValue(ErasedThroughField, out var raw) ||
            !uint.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var floor))
        {
            return new(false, 0, []);
        }

        var fingerprints = data.TryGetValue(ErasedKeyFingerprintsField, out var list)
            ? list.Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [];
        return new(true, floor, fingerprints);
    }

    /// <summary>
    /// Gets whether any of the fences covers a key. A key whose fingerprint cannot be worked out is covered.
    /// </summary>
    /// <param name="fences">The fences of its address.</param>
    /// <param name="revision">The revision of the key.</param>
    /// <param name="keyValue">The stored key value.</param>
    /// <returns>True when it is covered.</returns>
    public static bool AnyCovers(IReadOnlyList<LegacyErasureFence> fences, uint revision, string keyValue)
    {
        if (fences.Count == 0)
        {
            return false;
        }

        var fingerprint = FingerprintOf(keyValue);
        return fences.Any(fence => fence.Covers(revision, fingerprint));
    }

    static Dictionary<string, string>? Deserialize(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(value);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    static string? FingerprintOf(string keyValue)
    {
        try
        {
            var data = Deserialize(keyValue);
            return data is not null && data.TryGetValue(PublicKeyField, out var publicKey)
                ? new EncryptionKey(Convert.FromBase64String(publicKey), []).Fingerprint
                : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }

    bool Covers(uint revision, string? fingerprint) =>
        !Readable ||
        revision <= ErasedThrough ||
        fingerprint is null ||
        ErasedKeyFingerprints.Contains(fingerprint, StringComparer.Ordinal);
}
