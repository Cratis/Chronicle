// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Confidentiality;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.Compliance;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents a <see cref="IJsonSchemaMetadataValueHandler"/> for a globally-scoped, plain-confidentiality
/// <c language="csharp">[Encrypted]</c> value - <c language="csharp">EncryptionScope.Global</c>.
/// </summary>
/// <remarks>
/// <para>
/// One key protects every <c language="csharp">EncryptionScope.Global</c> value across the <b>whole installation</b> -
/// every event store, every namespace. Neither the per-document <c language="csharp">identifier</c> parameter nor the caller's real
/// <see cref="EventStoreName"/>/<see cref="EventStoreNamespaceName"/> are part of the key identity: every call is
/// routed through the fixed <see cref="EncryptedValueKeyIdentifiers.GlobalEventStore"/>/<see cref="EncryptedValueKeyIdentifiers.GlobalNamespace"/>
/// pair instead, so every namespace's global-scoped values converge on the one stored key.
/// </para>
/// <para>
/// This is not the compliance subject's boundary widened - it is a deliberately different, security-only scope
/// GDPR/PII never has: see <see cref="EncryptedSubjectValueHandler"/>'s remarks on why the two stay disjoint.
/// </para>
/// </remarks>
/// <param name="provisioner"><see cref="IManagedEncryptionKeyProvisioner"/> used to provision the installation-wide encryption-purpose key.</param>
/// <param name="encryptionKeyStore"><see cref="IEncryptionKeyStorage"/> to use for looking up keys on release.</param>
/// <param name="encryption"><see cref="IEncryption"/> for performing encryption/decryption.</param>
public class EncryptedGlobalValueHandler(
    IManagedEncryptionKeyProvisioner provisioner,
    IEncryptionKeyStorage encryptionKeyStore,
    IEncryption encryption) : IJsonSchemaMetadataValueHandler
{
    /// <inheritdoc/>
    public SchemaMetadataCategory Category => SchemaMetadataCategory.Security;

    /// <inheritdoc/>
    public SchemaMetadataTypeName Type => SecurityMetadataType.EncryptedGlobal.Value;

    /// <inheritdoc/>
    public Task<JsonNode> Apply(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        EncryptedValueOperations.Apply(
            provisioner,
            encryption,
            EncryptedValueKeyIdentifiers.GlobalEventStore,
            EncryptedValueKeyIdentifiers.GlobalNamespace,
            EncryptedValueKeyIdentifiers.ForGlobal(),
            value);

    /// <inheritdoc/>
    public Task<JsonNode> Release(EventStoreName eventStore, EventStoreNamespaceName eventStoreNamespace, string identifier, JsonNode value) =>
        EncryptedValueOperations.Release(
            encryptionKeyStore,
            encryption,
            EncryptedValueKeyIdentifiers.GlobalEventStore,
            EncryptedValueKeyIdentifiers.GlobalNamespace,
            EncryptedValueKeyIdentifiers.ForGlobal(),
            value);
}
