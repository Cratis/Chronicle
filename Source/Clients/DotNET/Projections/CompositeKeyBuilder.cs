// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using System.Text;
using Aksio.Reflection;
using Cratis.Reflection;

namespace Cratis.Projections;

/// <summary>
/// Represents an implementation of <see cref="ICompositeKeyBuilder{TKeyType, TEvent}"/>.
/// </summary>
/// <typeparam name="TKeyType">Type of key to build.</typeparam>
/// <typeparam name="TEvent">Event to build from.</typeparam>
public class CompositeKeyBuilder<TKeyType, TEvent> : ICompositeKeyBuilder<TKeyType, TEvent>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = new();

    /// <inheritdoc/>
    public ISetBuilder<TKeyType, TEvent, TProperty, ICompositeKeyBuilder<TKeyType, TEvent>> Set<TProperty>(Expression<Func<TKeyType, TProperty>> modelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var setBuilder = new SetBuilder<TKeyType, TEvent, TProperty, ICompositeKeyBuilder<TKeyType, TEvent>>(this, modelPropertyAccessor.GetPropertyPath(), !primitive);
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <inheritdoc/>
    public PropertyExpression Build()
    {
        var expressionBuilder = new StringBuilder();
        expressionBuilder
            .Append("$composite(")
            .AppendJoin(',', _propertyExpressions.Select(_ => $"{_.TargetProperty}={_.Build()}"))
            .Append(')');
        return expressionBuilder.ToString();
    }
}
