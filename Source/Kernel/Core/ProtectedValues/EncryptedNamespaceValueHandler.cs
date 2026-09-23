// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents a <see cref="IJsonCompliancePropertyValueHandler"/> for a namespace-scoped, plain-confidentiality
/// <c language="csharp">[Encrypted]</c> value - <c language="csharp">EncryptionScope.Namespace</c>.
/// </summary>
/// <remarks>
/// <para>
/// One key protects every <c language="csharp">EncryptionScope.Namespace</c> value in a given <see cref="EventStoreNamespaceName"/>,
/// regardless of which event, document, or subject it came from - unlike <see cref="EncryptedSubjectValueHandler"/>,
/// the per-document <c language="csharp">identifier</c> parameter is deliberately not part of the key identity. The real
/// <see cref="EventStoreName"/>/<see cref="EventStoreNamespaceName"/> the caller is already operating in is what
/// scopes the key to one namespace - exactly as it already scopes every other
/// <see cref="IEncryptionKeyStorage"/> operation - so <see cref="EncryptedValueKeyIdentifiers.ForNamespace"/>
/// itself carries no variable component.
/// </para>
/// <para>
/// This is not the compliance subject's boundary widened - it is a deliberately different, security-only scope
/// GDPR/PII never has: see <see cref="EncryptedSubjectValueHandler"/>'s remarks on why the two stay disjoint.
/// </para>
/// </remarks>
/// <param name="provisioner"><see cref="IManagedEncryptionKeyProvisioner"/> used to provision the namespace's encryption-purpose key.</param>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for looking up keys on release.</param>
/// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
public class EncryptedNamespaceValueHandler(
    IManagedEncryptionKeyProvisioner provisioner,
    IEncryptionKeyStorage encryptionKeyStore,
    IEncryption encryption) : IJsonCompliancePropertyValueHandler
{
    /// <inheritdoc/>
    public ComplianceMetadataType Type => ComplianceMetadataType.EncryptedNamespace;

    /// <inheritdoc/>
    public Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        EncryptedValueOperations.Apply(provisioner, encryption, eventStore, eventStoreNamespace, EncryptedValueKeyIdentifiers.ForNamespace(), value);

    /// <inheritdoc/>
    public Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        EncryptedValueOperations.Release(encryptionKeyStore, encryption, eventStore, eventStoreNamespace, EncryptedValueKeyIdentifiers.ForNamespace(), value);
}
