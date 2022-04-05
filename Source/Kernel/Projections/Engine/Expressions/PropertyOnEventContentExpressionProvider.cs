// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions;

/// <summary>
/// Represents a <see cref="IPropertyMapperExpressionResolver"/> for resolving value from a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
public class PropertyOnEventContentExpressionProvider : IPropertyMapperExpressionResolver
{
    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => !expression.StartsWith("$", StringComparison.InvariantCultureIgnoreCase);

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, string expression) => PropertyMappers.FromEventValueProvider(targetProperty, EventValueProviders.FromEventContent(expression));
}
