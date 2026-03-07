// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Represents a property and value for a unique constraint.
/// </summary>
/// <param name="Property">The name of the property.</param>
/// <param name="Value">The value of the property.</param>
public record UniqueConstraintPropertyAndValue(string Property, string Value);
