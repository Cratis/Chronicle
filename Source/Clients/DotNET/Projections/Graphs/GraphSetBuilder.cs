// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections.Graphs;

/// <summary>
/// Represents an implementation of <see cref="IGraphSetBuilder{TEvent, TSelf}"/>.
/// </summary>
/// <typeparam name="TEvent">The type of event.</typeparam>
/// <typeparam name="TSelf">The type of the builder, for continuation.</typeparam>
/// <param name="self">The builder to continue with.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> for naming properties.</param>
/// <param name="propertyName">The name of the property being mapped.</param>
/// <param name="properties">The property expressions to add to.</param>
/// <param name="function">The aggregating expression to wrap an event property in, or null to set the value.</param>
internal sealed class GraphSetBuilder<TEvent, TSelf>(TSelf self, INamingPolicy namingPolicy, string propertyName, Dictionary<string, string> properties, string? function) : IGraphSetBuilder<TEvent, TSelf>
{
    /// <inheritdoc/>
    public TSelf ToEventProperty(Expression<Func<TEvent, object?>> property)
    {
        var path = (string)ProjectionPropertyPaths.FromExpression(property, namingPolicy, ProjectionPathRole.Source);
        properties[propertyName] = function is null ? path : $"{function}({path})";
        return self;
    }

    /// <inheritdoc/>
    public TSelf ToEventContextProperty(Expression<Func<EventContext, object?>> property)
    {
        var path = (string)ProjectionPropertyPaths.FromExpression(property, namingPolicy, ProjectionPathRole.Source);
        properties[propertyName] = $"{WellKnownExpressions.EventContext}({path})";
        return self;
    }

    /// <inheritdoc/>
    public TSelf ToValue(object? value)
    {
        properties[propertyName] = value is null ? WellKnownExpressions.Null : $"{WellKnownExpressions.Value}({Convert.ToString(value, CultureInfo.InvariantCulture)})";
        return self;
    }
}
