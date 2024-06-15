// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Compliance.GDPR;

namespace Cratis.API.Compliance.PersonalInformation;

/// <summary>
/// Represents the concept of a last name of a person.
/// </summary>
/// <param name="Value">The actual string value.</param>
public record LastName(string Value) : PIIConceptAs<string>(Value);
