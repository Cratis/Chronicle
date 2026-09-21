// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IPropertyExpressionBuilder"/> for a fixed, well-known operation
/// expression (such as count, increment, or decrement) targeting a property in an "all" block.
/// </summary>
/// <param name="targetProperty">Target property we're building for.</param>
/// <param name="operationExpression">The well-known operation expression to use, from <see cref="WellKnownExpressions"/>.</param>
public class FromAllOperationBuilder(PropertyPath targetProperty, string operationExpression) : IPropertyExpressionBuilder
{
    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public string Build() => operationExpression;
}
