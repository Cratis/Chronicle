// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.Chronicle.Testing.ReadModels;

/// <summary>
/// A patient's email address. Personal data wherever it appears, so the marker sits on the concept and
/// travels into every event and read model that holds one.
/// </summary>
/// <param name="Value">The email address.</param>
[PII]
public record PatientEmailAddress(string Value) : ConceptAs<string>(Value)
{
    /// <summary>
    /// Implicitly converts a <see cref="string"/> to a <see cref="PatientEmailAddress"/>.
    /// </summary>
    /// <param name="value">The email address.</param>
    public static implicit operator PatientEmailAddress(string value) => new(value);
}
