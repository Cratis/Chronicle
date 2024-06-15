// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.API.Compliance.PersonalInformation;

/// <summary>
/// Represents the concept of an address.
/// </summary>
/// <param name="Value">Value of the address.</param>
public record Address(string Value) : PIIConceptAs<string>(Value);
