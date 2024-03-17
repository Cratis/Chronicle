// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.GDPR;

namespace Cratis.Kernel.Concepts.Compliance.PersonalInformation;

/// <summary>
/// Represents the concept of a last name of a person.
/// </summary>
/// <param name="Value">The actual string value.</param>
public record LastName(string Value) : PIIConceptAs<string>(Value);
