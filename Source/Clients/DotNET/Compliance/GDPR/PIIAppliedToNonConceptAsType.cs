// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Compliance.GDPR;

/// <summary>
/// The exception that is thrown when the <see cref="PIIAttribute"/> is applied to a type that does not inherit from <see cref="ConceptAs{T}"/>.
/// </summary>
/// <remarks>
/// The <see cref="PIIAttribute"/> is only supported on types that inherit from <see cref="ConceptAs{T}"/>.
/// </remarks>
/// <param name="type">The <see cref="Type"/> that has the attribute applied incorrectly.</param>
public class PIIAppliedToNonConceptAsType(Type type)
    : Exception($"The [PII] attribute is applied to '{type.FullName}', which does not inherit from ConceptAs<T>. The [PII] attribute is only supported on ConceptAs<T> types.")
{
}
