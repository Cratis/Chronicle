// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Grains.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an unknown constraint type is encountered.
/// </summary>
/// <param name="type">The type of the constraint.</param>
public class UnknownConstraintType(Type type) : Exception($"Unknown constraint type '{type.FullName}'");
