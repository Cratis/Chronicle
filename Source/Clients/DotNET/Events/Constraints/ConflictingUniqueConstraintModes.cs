// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when properties sharing a constraint name declare different retention modes.
/// </summary>
/// <param name="name">The name of the conflicting constraint.</param>
public class ConflictingUniqueConstraintModes(ConstraintName name) : Exception($"Unique constraint '{name}' declares conflicting retention modes.");
