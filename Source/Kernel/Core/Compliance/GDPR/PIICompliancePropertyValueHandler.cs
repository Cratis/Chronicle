// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for handling PII.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PIICompliancePropertyValueHandler"/>.
/// </remarks>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for keys.</param>
/// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
public class PIICompliancePropertyValueHandler(IEncryptionKeyStorage encryptionKeyStore, IEncryption encryption) : IJsonCompliancePropertyValueHandler
{
    static readonly ConcurrentDictionary<string, SemaphoreSlim> _keyCreationGates = new();

    readonly IEncryptionKeyStorage _encryptionKeyStore = encryptionKeyStore;
    readonly IEncryption _encryption = encryption;

    /// <inheritdoc/>
    public ComplianceMetadataType Type => ComplianceMetadataType.PII;

    /// <inheritdoc/>
    public async Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var key = await EnsureKeyFor(eventStore, eventStoreNamespace, identifier);
        var valueAsString = value.ToString();
        var encrypted = _encryption.Encrypt(Encoding.UTF8.GetBytes(valueAsString), key);
        var encryptedAsBase64 = Convert.ToBase64String(encrypted);
        return JsonValue.Create(encryptedAsBase64);
    }

    /// <inheritdoc/>
    public async Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var key = await _encryptionKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier);

        // When the encryption key has been deleted (GDPR right-to-erasure / crypto-shredding),
        // the PII is permanently unreadable. Surface it as empty rather than throwing so that
        // queries and read models for an erased subject keep working instead of crashing.
        if (key is null)
        {
            return JsonValue.Create(string.Empty);
        }

        // Only a value this encryption produced can be released. One that carries none of its shape was never
        // encrypted under this subject — it is resolved in memory at the query edge for display, or it predates
        // the property being marked [PII]. Releasing it is a no-op, so pass it through: blanking it would be
        // silent data loss indistinguishable from erasure, and throwing would fail an entire query over a
        // single property.
        if (!TryDecodeEncryptedValue(value.ToString(), out var encrypted))
        {
            return value;
        }

        var decrypted = _encryption.Decrypt(encrypted, key);
        var decryptedAsString = Encoding.UTF8.GetString(decrypted);
        return JsonValue.Create(decryptedAsString);
    }

    bool TryDecodeEncryptedValue(string value, out byte[] encrypted)
    {
        encrypted = [];

        // Base64 encodes four characters per three bytes, so anything else cannot be a value Apply produced.
        if (value.Length == 0 || value.Length % 4 != 0)
        {
            return false;
        }

        var buffer = new byte[value.Length / 4 * 3];
        if (!Convert.TryFromBase64String(value, buffer, out var bytesWritten))
        {
            return false;
        }

        var decoded = buffer[..bytesWritten];
        if (!_encryption.IsEncrypted(decoded))
        {
            return false;
        }

        encrypted = decoded;
        return true;
    }

    async Task<EncryptionKey> EnsureKeyFor(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, EncryptionKeyIdentifier identifier)
    {
        if (await _encryptionKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier) is { } existing)
        {
            return existing;
        }

        // A subject's key must be provisioned exactly once: a batch append and the sibling projections that
        // observe it all encrypt PII under the same subject concurrently (Task.WhenAll), and the same subject
        // may be provisioned from more than one silo. If two provisioners each generate and save a key, the
        // store mints a second revision and the value encrypted under the first key can no longer be decrypted
        // ("padding check failed"). The in-process gate serializes provisioning within this process to avoid
        // generating throwaway keys; GetOrAddFor is the atomic get-or-create that makes every provisioner
        // converge on a single persisted key pair even across processes / stale reads.
        var gate = _keyCreationGates.GetOrAdd($"{eventStore.Value}+{eventStoreNamespace.Value}+{identifier.Value}", _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return await _encryptionKeyStore.GetOrAddFor(eventStore, eventStoreNamespace, identifier, _encryption.GenerateKey());
        }
        finally
        {
            gate.Release();
        }
    }
}
