// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.PersonalInformation;

/// <summary>
/// Represents the concept of a value holding personal information.
/// </summary>
/// <param name="Value">The actual string value.</param>
public record PersonalInformationValue(string Value) : ConceptAs<string>(Value);
