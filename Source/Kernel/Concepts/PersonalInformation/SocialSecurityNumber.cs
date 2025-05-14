// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.PersonalInformation;

/// <summary>
/// Represents the concept of the social security number of a person.
/// </summary>
/// <param name="Value">The actual string value.</param>
public record SocialSecurityNumber(string Value) : ConceptAs<string>(Value);
