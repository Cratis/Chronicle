// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.Constraints;

/// <summary>
/// Exception that gets thrown when an unknown constraint type is encountered.
/// </summary>
/// <param name="definition">The unknown <see cref="IConstraintDefinition"/>.</param>
public class UnknownConstraintType(IConstraintDefinition definition) : Exception($"Unknown constraint type '{definition.GetType().Name}'")
{
}
