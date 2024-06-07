// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Compliance.GDPR;

namespace Cratis.PersonalInformation;

/// <summary>
/// Represents the concept of an city.
/// </summary>
/// <param name="Value">The actual string value.</param>
public record City(string Value) : PIIConceptAs<string>(Value);
