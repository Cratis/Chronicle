// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Declares the subject the personal data held by a read model property is released under, instead of the
/// read model's own compliance subject.
/// </summary>
/// <remarks>
/// A read model normally has exactly one compliance subject — an explicit <c>[Subject]</c>, otherwise the
/// property named <c>Id</c> — and every <see cref="PIIAttribute">[PII]</see> value on it is released under
/// that one subject. That is the right model for a read model Chronicle builds and stores: one document,
/// one erasable person.
/// <para>
/// It is not enough for a read model an application composes itself at the query edge, from rows it fetched
/// on its own. Such a row can carry a value that was encrypted under a subject that is not the row's own —
/// and until it can say so, the release pass has only two outcomes to offer it, both silent: released under
/// the wrong subject, or not released at all because no subject resolves and the value reaches the caller as
/// ciphertext. This attribute is how the row says which subject the value actually belongs to.
/// </para>
/// <code>
/// public record RetentionDueSubject(
///     SubjectId Person,
///     [ReleaseUnder(nameof(Person))] PostponementComment Comment);
/// </code>
/// <para>
/// The named property is looked up on the read model itself and its value is converted to a
/// <see cref="Subject"/> the same way an unadorned read model's subject is. Everything the adorned property
/// holds — a scalar, a value object, a collection — is released under that subject; every property without
/// the attribute keeps releasing under the read model's own subject, unchanged.
/// </para>
/// <para>
/// Declare it only on a property of the read model type itself. A declaration further down the graph, inside
/// a value object or a child element, cannot be honored and is reported as
/// <see cref="ReadModels.ReleaseUnderNotSupportedBelowReadModel"/> rather than ignored.
/// </para>
/// <para>
/// This does not make it acceptable to store one person's data on another person's document. It applies to a
/// value resolved in memory at the query edge; a stored read model still holds exactly one subject's personal
/// data, which is what keeps that person's erasure complete.
/// </para>
/// </remarks>
/// <param name="propertyName">Name of the property on the read model holding the subject to release under.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
public sealed class ReleaseUnderAttribute(string propertyName) : Attribute
{
    /// <summary>
    /// Gets the name of the property on the read model holding the subject to release under.
    /// </summary>
    public string PropertyName { get; } = propertyName;
}
