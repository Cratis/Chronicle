// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Exception that gets thrown when a with expression is missing in an add mapping.
/// </summary>
public class MissingSubtractWithExpression : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingToExpressionForAllSet"/> class.
    /// </summary>
    /// <param name="modelType">Type of model the expression is missing for.</param>
    /// <param name="propertyPath">Path within the model the expression is missing for.</param>
    public MissingSubtractWithExpression(Type modelType, PropertyPath propertyPath)
        : base($"Property '{propertyPath}' on '{modelType.FullName}' is missing a `.With())` expression when subtracting.")
    {
    }
}
