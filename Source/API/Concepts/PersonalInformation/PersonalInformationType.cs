// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Kernel.Concepts.Compliance.PersonalInformation;

/// <summary>
/// Represents the concept describing the type of personal information held by a <see cref="PersonalInformationValue"/> .
/// </summary>
/// <param name="Value">The actual string value.</param>
public record PersonalInformationType(string Value) : ConceptAs<string>(Value);
