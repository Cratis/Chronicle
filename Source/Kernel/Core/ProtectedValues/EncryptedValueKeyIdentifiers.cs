// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Builds the <see cref="EncryptionKeyIdentifier"/> a plain-confidentiality <c language="csharp">[Encrypted]</c> value is
/// provisioned and looked up under - deliberately disjoint from the identifier <see cref="Compliance.GDPR.PIICompliancePropertyValueHandler"/>
/// uses for the same subject.
/// </summary>
/// <remarks>
/// <para>
/// PII passes a subject's raw, unprefixed value straight through as its <see cref="EncryptionKeyIdentifier"/> -
/// that is <see cref="Compliance.GDPR.PIICompliancePropertyValueHandler"/>'s behavior today and this change does
/// not touch it. Without a second, disjoint identifier space, a subject that carries both a <c language="csharp">[PII]</c>
/// value and an <c language="csharp">[Encrypted]</c> value would land on <b>one</b> stored key - and
/// <see cref="Compliance.GDPR.IPIIManager.DeleteEncryptionKeyFor"/>, a lawful and routine erasure, would silently
/// destroy an operational secret that was never subject to the erasure request. This is the single worst outcome
/// available in the feature this identifier scheme exists to prevent.
/// </para>
/// <para>
/// <see cref="Marker"/> is reserved so that no bare subject value the PII path ever constructs can collide with
/// it: PII never prepends anything to the subject it is given, so any identifier carrying this marker could only
/// have been built here. <see cref="Compliance.GDPR.PIIManager"/> refuses an erasure or authorization addressed at
/// an identifier carrying the marker (see <see cref="EncryptionKeyIsNotErasable"/>) as a defense-in-depth backstop
/// for the one entry point - the compliance gRPC surface - that accepts an arbitrary caller-supplied identifier
/// rather than deriving one from a <see cref="Subject"/> itself.
/// </para>
/// </remarks>
public static class EncryptedValueKeyIdentifiers
{
    /// <summary>
    /// The reserved marker every <see cref="EncryptionKeyIdentifier"/> built by this type carries, and that the
    /// PII path never produces because it never prefixes the subject it is given.
    /// </summary>
    public const string Marker = "$chronicle-encrypted-value$";

    /// <summary>
    /// The fixed <see cref="EventStoreName"/> every <c language="csharp">EncryptionScope.Global</c> key is stored
    /// under, regardless of which real event store the protected value's event actually belongs to.
    /// </summary>
    /// <remarks>
    /// <see cref="Storage.Compliance.IEncryptionKeyStorage"/> keys every operation by <c language="csharp">(eventStore, eventStoreNamespace, identifier)</c>
    /// and that shape is not changed for this feature - see <see cref="EncryptedValueKeyIdentifiers"/>'s own
    /// remarks. A single key shared across every real event store and namespace is achieved by routing every
    /// <c language="csharp">EncryptionScope.Global</c> value's storage calls through this one fixed pair instead of the
    /// caller's actual <see cref="EventStoreName"/>/<see cref="EventStoreNamespaceName"/>, which is exactly as legitimate
    /// to the storage backend as any other event store name - it is namespaced string keys all the way down, with no
    /// pre-provisioning requirement.
    /// </remarks>
    public static readonly EventStoreName GlobalEventStore = $"{Marker}global-scope$";

    /// <summary>
    /// The fixed <see cref="EventStoreNamespaceName"/> every <c language="csharp">EncryptionScope.Global</c> key is
    /// stored under - see <see cref="GlobalEventStore"/>.
    /// </summary>
    public static readonly EventStoreNamespaceName GlobalNamespace = $"{Marker}global-scope$";

    /// <summary>
    /// Build the <see cref="EncryptionKeyIdentifier"/> a subject-scoped <c language="csharp">[Encrypted]</c> value is
    /// provisioned and looked up under.
    /// </summary>
    /// <param name="subjectIdentifier">The bare compliance identifier - the same value the PII path is given for the document.</param>
    /// <returns>The disjoint <see cref="EncryptionKeyIdentifier"/> for the subject's encryption-purpose key.</returns>
    public static EncryptionKeyIdentifier ForSubject(string subjectIdentifier) => new($"{Marker}subject${subjectIdentifier}");

    /// <summary>
    /// Build the <see cref="EncryptionKeyIdentifier"/> a namespace-scoped <c language="csharp">[Encrypted]</c> value is
    /// provisioned and looked up under. The identifier itself carries no variable component - the caller's real
    /// <see cref="EventStoreName"/>/<see cref="EventStoreNamespaceName"/> is what scopes it to one namespace, exactly
    /// as it already scopes every other <see cref="Storage.Compliance.IEncryptionKeyStorage"/> operation.
    /// </summary>
    /// <returns>The disjoint <see cref="EncryptionKeyIdentifier"/> for the namespace's encryption-purpose key.</returns>
    public static EncryptionKeyIdentifier ForNamespace() => new($"{Marker}namespace$");

    /// <summary>
    /// Build the <see cref="EncryptionKeyIdentifier"/> a globally-scoped <c language="csharp">[Encrypted]</c> value is
    /// provisioned and looked up under. Pair with <see cref="GlobalEventStore"/>/<see cref="GlobalNamespace"/> rather
    /// than the caller's real event store/namespace so every <c language="csharp">EncryptionScope.Global</c> value across the
    /// whole installation converges on the same stored key.
    /// </summary>
    /// <returns>The disjoint <see cref="EncryptionKeyIdentifier"/> for the installation-wide encryption-purpose key.</returns>
    public static EncryptionKeyIdentifier ForGlobal() => new($"{Marker}global$");

    /// <summary>
    /// Check whether an <see cref="EncryptionKeyIdentifier"/> is one this type builds.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to check.</param>
    /// <returns>True if the identifier carries the reserved marker, false otherwise.</returns>
    public static bool IsEncryptedValueIdentifier(EncryptionKeyIdentifier identifier) =>
        identifier.Value.StartsWith(Marker, StringComparison.Ordinal);
}
