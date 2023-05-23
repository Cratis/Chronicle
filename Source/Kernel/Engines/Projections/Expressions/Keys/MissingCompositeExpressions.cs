// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Projections;
using Aksio.Properties;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions.Keys;

/// <summary>
/// Exception that gets thrown when a composite key property mapping expression is invalid.
/// </summary>
public class MissingCompositeExpressions : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCompositeKeyPropertyMappingExpression"/> class.
    /// </summary>
    /// <param name="projectionId">Identifier of the projection.</param>
    /// <param name="identifiedByProperty">The property used as identifier for the key.</param>
    /// <param name="expression">Key/value expression.</param>
    public MissingCompositeExpressions(ProjectionId projectionId, PropertyPath identifiedByProperty, string expression) : base($"There are no property expressions in '{expression}' in projection '{projectionId}' for property '{identifiedByProperty}' is invalid. Expecting a collection key/value of property=expression separated by ,")
    {
    }
}
