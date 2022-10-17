// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Events.Projections.Expressions.ModelProperties;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for adding value on a model with the value for a property on the content of an <see cref="AppendedEvent"/>.
/// </summary>
public class SetExpressionResolver : IModelPropertyExpressionResolver
{
    readonly IEventValueProviderExpressionResolvers _eventValueProviderExpressionResolvers;

    /// <summary>
    /// Initializes a new instance of the <see cref="AddExpressionResolver"/> class.
    /// </summary>
    /// <param name="eventValueProviderExpressionResolvers"><see cref="IEventValueProviderExpressionResolvers"/> for resolving.</param>
    public SetExpressionResolver(IEventValueProviderExpressionResolvers eventValueProviderExpressionResolvers)
    {
        _eventValueProviderExpressionResolvers = eventValueProviderExpressionResolvers;
    }

    /// <inheritdoc/>
    public bool CanResolve(PropertyPath targetProperty, string expression) => _eventValueProviderExpressionResolvers.CanResolve(expression);

    /// <inheritdoc/>
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, string expression) => PropertyMappers.FromEventValueProvider(targetProperty, _eventValueProviderExpressionResolvers.Resolve(expression));
}
