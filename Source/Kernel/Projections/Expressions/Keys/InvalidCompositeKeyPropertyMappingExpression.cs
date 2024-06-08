// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;
using Cratis.Chronicle.Projections;

namespace Aksio.Cratis.Kernel.Projections.Expressions.Keys;

/// <summary>
/// Exception that gets thrown when a composite key property mapping expression is invalid.
/// </summary>
public class InvalidCompositeKeyPropertyMappingExpression : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCompositeKeyPropertyMappingExpression"/> class.
    /// </summary>
    /// <param name="projectionId">Identifier of the projection.</param>
    /// <param name="identifiedByProperty">The property used as identifier for the key.</param>
    /// <param name="expression">Key/value expression.</param>
    public InvalidCompositeKeyPropertyMappingExpression(ProjectionId projectionId, PropertyPath identifiedByProperty, string expression) : base($"Expression '{expression}' in projection '{projectionId}' for property '{identifiedByProperty}' is invalid. Expecting a key/value of property=expression")
    {
    }
}
