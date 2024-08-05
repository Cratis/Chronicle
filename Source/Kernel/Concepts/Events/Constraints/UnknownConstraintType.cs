// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Concepts.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an unknown constraint type is encountered.
/// </summary>
/// <remarks>
/// Initializes a new instance of <see cref="UnknownConstraintType"/>.
/// </remarks>
/// <param name="type"><see cref="Type"/> that is unknown.</param>
public class UnknownConstraintType(Type type) : Exception($"Unknown constraint type '{type.FullName}'");
