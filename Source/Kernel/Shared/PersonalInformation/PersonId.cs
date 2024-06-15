// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.PersonalInformation;

/// <summary>
/// Represents the concept of a unique identifier that identifies a person.
/// </summary>
/// <param name="Value">Underlying value.</param>
public record PersonId(string Value) : ConceptAs<string>(Value);
