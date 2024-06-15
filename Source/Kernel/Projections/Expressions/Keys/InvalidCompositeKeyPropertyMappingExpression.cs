// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Projections;
using Cratis.Properties;

namespace Cratis.Chronicle.Projections.Expressions.Keys;

/// <summary>
/// Exception that gets thrown when a composite key property mapping expression is invalid.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InvalidCompositeKeyPropertyMappingExpression"/> class.
/// </remarks>
/// <param name="projectionId">Identifier of the projection.</param>
/// <param name="identifiedByProperty">The property used as identifier for the key.</param>
/// <param name="expression">Key/value expression.</param>
public class InvalidCompositeKeyPropertyMappingExpression(ProjectionId projectionId, PropertyPath identifiedByProperty, string expression) : Exception($"Expression '{expression}' in projection '{projectionId}' for property '{identifiedByProperty}' is invalid. Expecting a key/value of property=expression")
{
}
