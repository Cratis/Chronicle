// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance;
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
    /// Build the <see cref="EncryptionKeyIdentifier"/> a subject-scoped <c language="csharp">[Encrypted]</c> value is
    /// provisioned and looked up under.
    /// </summary>
    /// <param name="subjectIdentifier">The bare compliance identifier - the same value the PII path is given for the document.</param>
    /// <returns>The disjoint <see cref="EncryptionKeyIdentifier"/> for the subject's encryption-purpose key.</returns>
    public static EncryptionKeyIdentifier ForSubject(string subjectIdentifier) => new($"{Marker}subject${subjectIdentifier}");

    /// <summary>
    /// Check whether an <see cref="EncryptionKeyIdentifier"/> is one this type builds.
    /// </summary>
    /// <param name="identifier"><see cref="EncryptionKeyIdentifier"/> to check.</param>
    /// <returns>True if the identifier carries the reserved marker, false otherwise.</returns>
    public static bool IsEncryptedValueIdentifier(EncryptionKeyIdentifier identifier) =>
        identifier.Value.StartsWith(Marker, StringComparison.Ordinal);
}
