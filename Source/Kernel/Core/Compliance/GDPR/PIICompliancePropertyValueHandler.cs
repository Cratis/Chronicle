// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.ProtectedValues;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Represents a <see cref="IJsonSchemaMetadataValueHandler"/> for handling PII.
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
    IEncryption encryption) : IJsonSchemaMetadataValueHandler
{
    /// <inheritdoc/>
    public SchemaMetadataCategory Category => SchemaMetadataCategory.Compliance;

    /// <inheritdoc/>
    public SchemaMetadataTypeName Type => ComplianceMetadataType.PII.Value;

    /// <inheritdoc/>
    public async Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var key = await provisioner.EnsureKeyFor(eventStore, eventStoreNamespace, identifier);
        return ProtectedValueCodec.Encrypt(encryption, key, value);
    }

    /// <inheritdoc/>
    public async Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        (await ReleaseWithStatus(eventStore, eventStoreNamespace, identifier, value)).Value;

    /// <inheritdoc/>
    public async Task<ReleasedSchemaMetadataValue> ReleaseWithStatus(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        if (!ProtectedValueCodec.TryDecodeCipherText(encryption, value.ToString(), out var encrypted))
        {
            // Legacy and display-only plaintext can survive for a live subject, but absence of ciphertext
            // is not permission to release erased PII. Even a later authorized key cannot date plaintext
            // to the new lifecycle, so only encrypted values can establish that distinction.
            return await encryptionKeyStore.GetErasureFor(eventStore, eventStoreNamespace, identifier) is not null
                ? new(JsonValue.Create(string.Empty), IsUnreadable: true)
                : new(value);
        }

        var key = await encryptionKeyStore.TryGetFor(eventStore, eventStoreNamespace, identifier);

        // When the encryption key has been deleted (GDPR right-to-erasure / crypto-shredding),
        // the PII is permanently unreadable. Signal that separately from decrypted empty plaintext so
        // the schema manager can supply a typed placeholder without changing a live subject's empty string.
        if (key is null)
        {
            return new(JsonValue.Create(string.Empty), IsUnreadable: true);
        }

        return new(ProtectedValueCodec.Decrypt(encryption, key, encrypted));
    }
}
