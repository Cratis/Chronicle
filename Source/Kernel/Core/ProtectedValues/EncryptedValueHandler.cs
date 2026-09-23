// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for a subject-scoped, plain-confidentiality
/// <c language="csharp">[Encrypted]</c> value.
/// </summary>
/// <remarks>
/// <para>
/// This is the security counterpart to <see cref="Compliance.GDPR.PIICompliancePropertyValueHandler"/>, built on
/// the same neutral, already-generic machinery: <see cref="IManagedEncryptionKeyProvisioner"/> for provisioning
/// and <see cref="ProtectedValueCodec"/> for the at-rest shape. What is deliberately <b>not</b> shared is the key
/// identity - see <see cref="EncryptedValueKeyIdentifiers"/> - and there is no erasure path here at all: unlike
/// <see cref="Compliance.GDPR.IPIIManager"/>, nothing on this type, or reachable from it, can delete the key this
/// handler provisions. A key protecting a secret with no data subject has no lawful basis to be destroyed on
/// request, and the absence of a delete method is what makes that a property of the type rather than a discipline
/// callers have to maintain.
/// </para>
/// <para>
/// Today the only scope this handler protects is <see cref="ComplianceMetadataType.EncryptedSubject"/> - the
/// <c language="csharp">EncryptionScope.Subject</c> client-side default. Namespace- and installation-scoped keys are a
/// deliberately separate, not-yet-implemented extension: they need a key identity that does not depend on a
/// document carrying a subject at all, and the release paths that decide whether to even attempt a release
/// currently assume one is present. Extending this feature to those scopes is tracked separately rather than
/// bundled into the identifier-separation and provisioning work this type delivers.
/// </para>
/// </remarks>
/// <param name="provisioner"><see cref="IManagedEncryptionKeyProvisioner"/> used to provision the subject's encryption-purpose key.</param>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for looking up keys on release.</param>
/// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
public class EncryptedValueHandler(
    IManagedEncryptionKeyProvisioner provisioner,
    IEncryptionKeyStorage encryptionKeyStore,
    IEncryption encryption) : IJsonCompliancePropertyValueHandler
{
    /// <inheritdoc/>
    public ComplianceMetadataType Type => ComplianceMetadataType.EncryptedSubject;

    /// <inheritdoc/>
    public async Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        var keyIdentifier = EncryptedValueKeyIdentifiers.ForSubject(identifier);
        var key = await provisioner.EnsureKeyFor(eventStore, eventStoreNamespace, keyIdentifier);
        return ProtectedValueCodec.Encrypt(encryption, key, value);
    }

    /// <inheritdoc/>
    public async Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value)
    {
        // Same reasoning as the PII release path: a value carrying none of this encryption's shape was never
        // protected under this key - display-only resolution, or data written before the property was marked - so
        // releasing it is a pass-through rather than an error.
        if (!ProtectedValueCodec.TryDecodeCipherText(encryption, value.ToString(), out var encrypted))
        {
            return value;
        }

        var keyIdentifier = EncryptedValueKeyIdentifiers.ForSubject(identifier);
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
