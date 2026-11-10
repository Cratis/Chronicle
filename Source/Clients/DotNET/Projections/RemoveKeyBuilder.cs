// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents a <see cref="IPropertyExpressionBuilder"/> that removes a dictionary entry addressed by a dynamic key.
/// </summary>
/// <param name="targetProperty">The dictionary entry to remove, including its dynamic key segment.</param>
public class RemoveKeyBuilder(PropertyPath targetProperty) : IPropertyExpressionBuilder
{
    /// <inheritdoc/>
    public PropertyPath TargetProperty { get; } = targetProperty;

    /// <inheritdoc/>
    public string Build() => WellKnownExpressions.Remove;
}
