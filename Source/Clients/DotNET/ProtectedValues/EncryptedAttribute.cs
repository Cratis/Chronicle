// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ProtectedValues;

/// <summary>
/// Represents an attribute that can be used to mark a class or property as needing plain-confidentiality
/// encryption at rest - a security measure, not a compliance one.
/// </summary>
/// <remarks>
/// <para>
/// Use <see cref="EncryptedAttribute"/> for an operational secret that has no data subject and no lawful basis
/// for erasure - an API key, a webhook signing secret, a partner credential. Use
/// <see cref="Compliance.GDPR.PIIAttribute"/> instead when the value is personal data about a natural person:
/// only <c language="csharp">[PII]</c> encrypts a value <em>and</em> enrolls it in GDPR right-to-erasure. Marking a secret
/// <c language="csharp">[PII]</c> would make it erasable on a request that was never about it; marking personal data
/// <c language="csharp">[Encrypted]</c> would encrypt it but never erase it. The two are not interchangeable, and this
/// attribute's key is never provisioned under the same identity a <c language="csharp">[PII]</c> value for the same
/// subject uses - see <c language="csharp">EncryptedValueKeyIdentifiers</c> on the kernel side for why.
/// </para>
/// <para>
/// Today only <see cref="EncryptionScope.Subject"/> is honored by the kernel. A property marked with
/// <see cref="EncryptionScope.Namespace"/> or <see cref="EncryptionScope.Global"/> is accepted here - the
/// attribute compiles and the schema is generated - but is rejected at metadata-provision time with a clear
/// exception rather than silently falling back to a scope that was not asked for. This is deliberate: the two
/// wider scopes need a key identity that does not depend on a document carrying a subject at all, which is a
/// separate, larger change than establishing the disjoint key identity this attribute delivers today.
/// </para>
/// </remarks>
/// <remarks>
/// Initializes a new instance of the <see cref="EncryptedAttribute"/> class.
/// </remarks>
/// <param name="scope">The <see cref="EncryptionScope"/> the key is provisioned under - defaults to <see cref="EncryptionScope.Subject"/>.</param>
/// <param name="details">Optional details - default value is empty string.</param>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class EncryptedAttribute(EncryptionScope scope = EncryptionScope.Subject, string details = "") : Attribute
{
    /// <summary>
    /// Gets the <see cref="EncryptionScope"/> the type or property marked is encrypted under.
    /// </summary>
    public EncryptionScope Scope { get; } = scope;

    /// <summary>
    /// Gets the details as to why or to what purpose/extent the type or property marked needs encryption.
    /// </summary>
    public string Details { get; } = details;
}
