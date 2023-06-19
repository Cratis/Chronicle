// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Projections;

/// <summary>
/// Exception that gets thrown when a to expression is missing in a mapping.
/// </summary>
public class MissingToExpressionForAllSet : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MissingToExpressionForAllSet"/> class.
    /// </summary>
    /// <param name="modelType">Type of model the expression is missing for.</param>
    /// <param name="propertyPath">Path within the model the expression is missing for.</param>
    public MissingToExpressionForAllSet(Type modelType, PropertyPath propertyPath)
        : base($"Property '{propertyPath}' on '{modelType.FullName}' is missing a `.To...()` expression when mapping in an all expression")
    {
    }
}
