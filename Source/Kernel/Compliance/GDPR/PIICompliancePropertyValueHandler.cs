// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Aksio.Cratis.Compliance;
using Aksio.Cratis.Kernel.Storage.Compliance;

namespace Aksio.Cratis.Kernel.Compliance.GDPR;

/// <summary>
/// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for handling PII.
/// </summary>
public class PIICompliancePropertyValueHandler : IJsonCompliancePropertyValueHandler
{
    readonly IEncryptionKeyStorage _encryptionKeyStore;
    readonly IEncryption _encryption;

    /// <summary>
    /// Initializes a new instance of the <see cref="PIICompliancePropertyValueHandler"/>.
    /// </summary>
    /// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for keys.</param>
    /// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
    public PIICompliancePropertyValueHandler(IEncryptionKeyStorage encryptionKeyStore, IEncryption encryption)
    {
        _encryptionKeyStore = encryptionKeyStore;
        _encryption = encryption;
    }

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
