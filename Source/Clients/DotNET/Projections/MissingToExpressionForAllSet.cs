// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Properties;

namespace Cratis.Projections;

/// <summary>
/// Exception that gets thrown when a to expression is missing in a mapping.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="MissingToExpressionForAllSet"/> class.
/// </remarks>
/// <param name="modelType">Type of model the expression is missing for.</param>
/// <param name="propertyPath">Path within the model the expression is missing for.</param>
public class MissingToExpressionForAllSet(Type modelType, PropertyPath propertyPath)
    : Exception($"Property '{propertyPath}' on '{modelType.FullName}' is missing a `.To...()` expression when mapping in an all expression");
