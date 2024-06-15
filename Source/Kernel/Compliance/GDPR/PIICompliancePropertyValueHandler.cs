// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
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
    readonly IEncryptionKeyStorage _encryptionKeyStore = encryptionKeyStore;
    readonly IEncryption _encryption = encryption;

    /// <inheritdoc/>
    public ComplianceMetadataType Type => ComplianceMetadataType.PII;

    /// <inheritdoc/>
    public async Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var key = await _encryptionKeyStore.GetFor(eventStore, eventStoreNamespace, identifier);
        var valueAsString = value.ToString();
        var encrypted = _encryption.Encrypt(Encoding.UTF8.GetBytes(valueAsString), key);
        var encryptedAsBase64 = Convert.ToBase64String(encrypted);
        return JsonValue.Create(encryptedAsBase64)!;
    }

    /// <inheritdoc/>
    public async Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var key = await _encryptionKeyStore.GetFor(eventStore, eventStoreNamespace, identifier);
        var encryptedAsString = value.ToString();
        var encrypted = Convert.FromBase64String(encryptedAsString);
        var decrypted = _encryption.Decrypt(encrypted, key);
        var decryptedAsString = Encoding.UTF8.GetString(decrypted);
        return JsonValue.Create(decryptedAsString)!;
    }
}
