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
/// All three <see cref="EncryptionScope"/> members are honored by the kernel: <see cref="EncryptionScope.Subject"/>
/// (the default) provisions a key per compliance identity, exactly matching how a <c language="csharp">[PII]</c> value on the
/// same document is resolved; <see cref="EncryptionScope.Namespace"/> provisions one key shared by every value marked
/// with it in a given event store namespace; <see cref="EncryptionScope.Global"/> provisions one key shared across the
/// whole installation. Choosing a wider scope is safe for <c language="csharp">[Encrypted]</c> in a way it is not for
/// <c language="csharp">[PII]</c>, precisely because an operational secret has no data subject whose erasure request the
/// wider key could ever need to honor separately from another subject's.
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
