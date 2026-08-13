// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// Declares the subject the personal data held by a read model property is released under, instead of the
/// read model's own compliance subject.
/// </summary>
/// <remarks>
/// Superseded by <see cref="SubjectFromAttribute">[SubjectFrom]</see>, which says the same thing the way the
/// rest of the model says it: declaratively, naming where the subject is found rather than the action taken
/// with it. This type stays, deriving from <see cref="SubjectFromAttribute"/>, so read models written against
/// the released name keep compiling and keep behaving identically — everything that reads a declaration reads
/// <see cref="SubjectFromAttribute"/>, and finds this one through it.
/// <para>
/// The deprecation sits on the constructor rather than the type, because applying the attribute is the only
/// way to consume it — that is where the warning belongs, and it keeps the type nameable without warning by
/// the reflection and type-discovery code that has to enumerate every attribute in the assembly.
/// </para>
/// </remarks>
/// <param name="propertyName">Name of the property on the read model holding the subject to use.</param>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = false)]
[method: Obsolete("Use [SubjectFrom] instead. [ReleaseUnder] is kept as an alias of [SubjectFrom] and behaves identically.", false)]
public sealed class ReleaseUnderAttribute(string propertyName) : SubjectFromAttribute(propertyName);
