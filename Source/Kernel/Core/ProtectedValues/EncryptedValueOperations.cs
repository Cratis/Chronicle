// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// The apply/release mechanics every plain-confidentiality <c language="csharp">[Encrypted]</c> value handler uses,
/// regardless of scope. What varies per scope is only <em>which</em> <see cref="EncryptionKeyIdentifier"/> - and,
/// for <c language="csharp">EncryptionScope.Global</c>, which <see cref="EventStoreName"/>/<see cref="EventStoreNamespaceName"/>
/// - it is provisioned and looked up under; see <see cref="EncryptedSubjectValueHandler"/>,
/// <see cref="EncryptedNamespaceValueHandler"/> and <see cref="EncryptedGlobalValueHandler"/>.
/// </summary>
static class EncryptedValueOperations
{
    /// <summary>
    /// Encrypt a value under the given key identity, provisioning the key on first use.
    /// </summary>
    /// <param name="provisioner"><see cref="IManagedEncryptionKeyProvisioner"/> used to provision the key.</param>
    /// <param name="encryption"><see cref="IEncryption"/> for performing the encryption.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> to provision the key under.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> to provision the key under.</param>
    /// <param name="keyIdentifier">The <see cref="EncryptionKeyIdentifier"/> to provision and encrypt under.</param>
    /// <param name="value">The <see cref="JsonNode"/> to encrypt.</param>
    /// <returns>The encrypted <see cref="JsonNode"/>.</returns>
    public static async Task<JsonNode> Apply(
        IManagedEncryptionKeyProvisioner provisioner,
        IEncryption encryption,
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier keyIdentifier,
        JsonNode value)
    {
        var key = await provisioner.EnsureKeyFor(eventStore, eventStoreNamespace, keyIdentifier);
        return ProtectedValueCodec.Encrypt(encryption, key, value);
    }

    /// <summary>
    /// Decrypt a value that was encrypted under the given key identity.
    /// </summary>
    /// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for looking up the key.</param>
    /// <param name="encryption"><see cref="IEncryption"/> for performing the decryption.</param>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the key was provisioned under.</param>
    /// <param name="eventStoreNamespace">The <see cref="EventStoreNamespaceName"/> the key was provisioned under.</param>
    /// <param name="keyIdentifier">The <see cref="EncryptionKeyIdentifier"/> the value was encrypted under.</param>
    /// <param name="value">The <see cref="JsonNode"/> to decrypt.</param>
    /// <returns>The decrypted <see cref="JsonNode"/>.</returns>
    public static async Task<JsonNode> Release(
        IEncryptionKeyStorage encryptionKeyStore,
        IEncryption encryption,
        EventStoreName eventStore,
        EventStoreNamespaceName eventStoreNamespace,
        EncryptionKeyIdentifier keyIdentifier,
        JsonNode value)
    {
        // A value carrying none of this encryption's shape was never protected under this key - display-only
        // resolution, or data written before the property was marked - so releasing it is a pass-through rather
        // than an error.
        if (!ProtectedValueCodec.TryDecodeCipherText(encryption, value.ToString(), out var encrypted))
        {
            return value;
        }

        var key = await encryptionKeyStore.TryGetFor(eventStore, eventStoreNamespace, keyIdentifier);

        // Unlike PII, an [Encrypted] key is never deleted, so a missing key here is not an erasure outcome - it
        // means the value was encrypted by a process this store has no record of (a restored backup pointed at a
        // different key store, for example). Surfacing it as empty rather than throwing keeps a query over the
        // rest of the document working, exactly as the PII path already does for its own missing-key case.
        if (key is null)
        {
            return JsonValue.Create(string.Empty);
        }

        return ProtectedValueCodec.Decrypt(encryption, key, encrypted);
    }
}
