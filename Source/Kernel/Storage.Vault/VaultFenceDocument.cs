// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Storage.Vault;

/// <summary>
/// Encodes fence semantics independently of Vault's native CAS version. Unknown fields are ignored.
/// </summary>
internal static class VaultFenceDocument
{
    static readonly string[] _protocolFields = ["version", "lifecycle", "erasedLifecycle", "requests", "seal"];

    /// <summary>Encode and seal canonical fence semantics.</summary>
    /// <param name="fence">The fence.</param>
    /// <returns>The KV data.</returns>
    internal static Dictionary<string, object> Write(EncryptionKeyErasure fence) => new()
    {
        ["erasedThrough"] = fence.ErasedThrough.Value.ToString(CultureInfo.InvariantCulture),
        ["erasedKeyFingerprints"] = string.Join(',', fence.ErasedKeyFingerprints),
        ["newKeyAllowed"] = fence.NewKeyAllowed.ToString(),
        ["version"] = fence.Version.ToString(CultureInfo.InvariantCulture),
        ["lifecycle"] = fence.Lifecycle.ToString(CultureInfo.InvariantCulture),
        ["erasedLifecycle"] = fence.ErasedLifecycle.ToString(CultureInfo.InvariantCulture),
        ["requests"] = JsonSerializer.Serialize(fence.Requests ?? []),
        ["seal"] = Seal(fence),
        ["layout"] = VaultPathSegments.Root
    };

    /// <summary>Validate new fences or conservatively interpret an entirely legacy shape.</summary>
    /// <param name="identifier">The subject.</param>
    /// <param name="data">The KV data.</param>
    /// <returns>The fence.</returns>
    /// <exception cref="EncryptionKeyFenceUnavailable">The fence is malformed or its seal is invalid.</exception>
    internal static EncryptionKeyErasure Read(EncryptionKeyIdentifier identifier, IDictionary<string, object> data)
    {
        try
        {
            string Required(string name) => data.TryGetValue(name, out var value) && value is not null ? value.ToString()! : throw new EncryptionKeyFenceUnavailable(identifier);
            var legacy = _protocolFields.All(field => !data.ContainsKey(field));
            var fence = new EncryptionKeyErasure(
                uint.Parse(Required("erasedThrough"), CultureInfo.InvariantCulture),
                Required("erasedKeyFingerprints").Split(',', StringSplitOptions.RemoveEmptyEntries),
                bool.Parse(Required("newKeyAllowed")),
                legacy ? 0 : ulong.Parse(Required("version"), CultureInfo.InvariantCulture),
                legacy ? 1 : uint.Parse(Required("lifecycle"), CultureInfo.InvariantCulture),
                legacy ? 0 : uint.Parse(Required("erasedLifecycle"), CultureInfo.InvariantCulture),
                legacy ? [] : JsonSerializer.Deserialize<EncryptionKeyErasureRequest[]>(Required("requests")),
                legacy ? null : Required("seal"));
            if (!legacy && (fence.Requests is null || fence.Lifecycle == 0 || !StringComparer.Ordinal.Equals(fence.Seal, Seal(fence))))
            {
                throw new EncryptionKeyFenceUnavailable(identifier);
            }

            return fence;
        }
        catch (Exception exception) when (exception is FormatException or OverflowException or JsonException or ArgumentException)
        {
            throw new EncryptionKeyFenceUnavailable(identifier);
        }
    }

    static string Seal(EncryptionKeyErasure fence) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
    {
        Through = fence.ErasedThrough.Value,
        Fingerprints = fence.ErasedKeyFingerprints.Order(StringComparer.Ordinal).ToArray(),
        fence.NewKeyAllowed,
        fence.Version,
        fence.Lifecycle,
        fence.ErasedLifecycle,
        Requests = (fence.Requests ?? []).OrderBy(_ => _.RequestId, StringComparer.Ordinal).Select(_ => new
        {
            _.RequestId,
            Through = _.Through?.Value,
            _.Lifecycle,
            Fingerprints = (_.Fingerprints ?? []).Order(StringComparer.Ordinal).ToArray(),
            _.Confirmed
        }).ToArray()
    }))));
}
