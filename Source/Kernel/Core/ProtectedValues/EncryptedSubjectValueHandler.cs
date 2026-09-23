// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for a subject-scoped, plain-confidentiality
/// <c language="csharp">[Encrypted]</c> value - <c language="csharp">EncryptionScope.Subject</c>, the client-side default.
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
/// See <see cref="EncryptedNamespaceValueHandler"/> and <see cref="EncryptedGlobalValueHandler"/> for the other two
/// scopes. All three share their apply/release mechanics through <see cref="EncryptedValueOperations"/> - what
/// differs between them is only which <see cref="Compliance.EncryptionKeyIdentifier"/> (and, for the global scope,
/// which fixed <see cref="EventStoreName"/>/<see cref="EventStoreNamespaceName"/>) the key is provisioned under.
/// </para>
/// </remarks>
/// <param name="provisioner"><see cref="IManagedEncryptionKeyProvisioner"/> used to provision the subject's encryption-purpose key.</param>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for looking up keys on release.</param>
/// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
public class EncryptedSubjectValueHandler(
    IManagedEncryptionKeyProvisioner provisioner,
    IEncryptionKeyStorage encryptionKeyStore,
    IEncryption encryption) : IJsonCompliancePropertyValueHandler
{
    /// <inheritdoc/>
    public ComplianceMetadataType Type => ComplianceMetadataType.EncryptedSubject;

    /// <inheritdoc/>
    public Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        EncryptedValueOperations.Apply(provisioner, encryption, eventStore, eventStoreNamespace, EncryptedValueKeyIdentifiers.ForSubject(identifier), value);

    /// <inheritdoc/>
    public Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        EncryptedValueOperations.Release(encryptionKeyStore, encryption, eventStore, eventStoreNamespace, EncryptedValueKeyIdentifiers.ForSubject(identifier), value);
}
