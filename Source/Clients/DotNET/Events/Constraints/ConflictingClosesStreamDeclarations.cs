// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// The exception that is thrown when same-named closing declarations disagree on scope or payload property.
/// </summary>
/// <param name="name">The conflicting constraint name.</param>
public class ConflictingClosesStreamDeclarations(ConstraintName name) : Exception($"Closing declarations for '{name}' must use the same dimensions and stream identifier property.");
