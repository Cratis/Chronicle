// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Exception that gets thrown when a with expression is missing in an add mapping.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingToExpressionForAllSet"/> class.
/// </remarks>
/// <param name="modelType">Type of model the expression is missing for.</param>
/// <param name="propertyPath">Path within the model the expression is missing for.</param>
public class MissingSubtractWithExpression(Type modelType, PropertyPath propertyPath)
    : Exception($"Property '{propertyPath}' on '{modelType.FullName}' is missing a `.With())` expression when subtracting.");
