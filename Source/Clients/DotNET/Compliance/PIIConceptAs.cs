// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Compliance
{
    /// <summary>
    /// Represents a <see cref="ConceptAs{T}"/> that holds PII according to the definition of GDPR.
    /// </summary>
    /// <param name="Value">Underlying value.</param>
    /// <typeparam name="T">Type of the underlying value.</typeparam>
    public record PIIConceptAs<T>(T Value) : ConceptAs<T>(Value), IHoldPII;
}
