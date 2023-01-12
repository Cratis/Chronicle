// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Kernel.Engines.Projections.Expressions.EventValues;
using Aksio.Cratis.Properties;
using Aksio.Cratis.Events;
using NJsonSchema;

namespace Aksio.Cratis.Kernel.Engines.Projections.Expressions.ModelProperties;

/// <summary>
/// Represents a <see cref="IModelPropertyExpressionResolver"/> for setting a property on a model with the value for a property based event value expressions.
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
    public PropertyMapper<AppendedEvent, ExpandoObject> Resolve(PropertyPath targetProperty, JsonSchemaProperty targetPropertySchema, string expression) =>
        PropertyMappers.FromEventValueProvider(targetProperty, _eventValueProviderExpressionResolvers.Resolve(targetPropertySchema, expression));
}
