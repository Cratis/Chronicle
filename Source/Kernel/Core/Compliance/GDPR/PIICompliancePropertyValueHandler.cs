// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for handling PII.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PIICompliancePropertyValueHandler"/>.
/// </remarks>
/// <param name="provisioner"><see cref="IManagedEncryptionKeyProvisioner"/> used to provision the subject's key.</param>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for keys.</param>
/// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
public class PIICompliancePropertyValueHandler(
    IManagedEncryptionKeyProvisioner provisioner,
    IEncryptionKeyStorage encryptionKeyStore,
    IEncryption encryption) : IJsonCompliancePropertyValueHandler
{
    /// <inheritdoc/>
    public ComplianceMetadataType Type => ComplianceMetadataType.PII;

    /// <inheritdoc/>
    public async Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var key = await provisioner.EnsureKeyFor(eventStore, eventStoreNamespace, identifier);
        return ProtectedValueCodec.Encrypt(encryption, key, value);
    }

    /// <inheritdoc/>
    public async Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        // Only a value this encryption produced can be released. One that carries none of its shape was never
        // encrypted under this subject — it is resolved in memory at the query edge for display, or it predates
        // the property being marked [PII]. Releasing it is a no-op, so pass it through: blanking it would be
        // silent data loss indistinguishable from erasure, and throwing would fail an entire query over a
        // single property.
        //
        // Asked before the key, because whether the subject holds a key answers "can this be decrypted" and the
        // question here is "was this ever encrypted". A key is only ever minted for a subject that encrypts
        // something at rest, so a read model keyed by a hash, a cluster identifier or any other computed identity
        // has none — and its display-only values were being emptied on every read. Erasure is unaffected:
        // IsEncrypted takes no key, so a genuinely encrypted value whose key has been shredded still answers yes
        // here, still falls through to the key lookup, and still blanks.
        if (!ProtectedValueCodec.TryDecodeCipherText(encryption, value.ToString(), out var encrypted))
        {
            return value;
        }

        var key = await encryptionKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier);

        // When the encryption key has been deleted (GDPR right-to-erasure / crypto-shredding),
        // the PII is permanently unreadable. Surface it as empty rather than throwing so that
        // queries and read models for an erased subject keep working instead of crashing.
        if (key is null)
        {
            return JsonValue.Create(string.Empty);
        }

        return ProtectedValueCodec.Decrypt(encryption, key, encrypted);
    }
}
