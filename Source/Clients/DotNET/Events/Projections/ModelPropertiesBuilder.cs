// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Aksio.Cratis.Concepts;
using Aksio.Cratis.Events.Store;
using Aksio.Cratis.Reflection;

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertiesBuilder{TModel, TEvent, TBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public class ModelPropertiesBuilder<TModel, TEvent, TBuilder> : IModelPropertiesBuilder<TModel, TEvent, TBuilder>
    where TBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TBuilder>
{
    #pragma warning disable CA1051 // Visible instance fields
    #pragma warning disable SA1600 // Elements should be documented
    #pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly List<IPropertyExpressionBuilder> _propertyExpressions = new();
    protected string? _parentKey;
    protected string? _key;
    #pragma warning restore CA1629, CA1002, MA0016 // Return abstract
    #pragma warning restore CA1600 // Elements should be documented

    /// <inheritdoc/>
    public TBuilder UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _key = keyAccessor.GetPropertyPath();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _parentKey = keyAccessor.GetPropertyPath();
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingCompositeKey(params Expression<Func<TEvent, object>>[] keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public TBuilder UsingCompositeKeyFromContext(params Expression<Func<EventContext, object>>[] keyAccessor) => throw new NotImplementedException();

    /// <inheritdoc/>
    public IAddBuilder<TModel, TEvent, TProperty, TBuilder> Add<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var addBuilder = new AddBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, modelPropertyAccessor.GetPropertyPath());
        _propertyExpressions.Add(addBuilder);
        return addBuilder;
    }

    /// <inheritdoc/>
    public ISubtractBuilder<TModel, TEvent, TProperty, TBuilder> Subtract<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var subtractBuilder = new SubtractBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, modelPropertyAccessor.GetPropertyPath());
        _propertyExpressions.Add(subtractBuilder);
        return subtractBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var setBuilder = new SetBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, modelPropertyAccessor.GetPropertyPath(), !primitive);
        _propertyExpressions.Add(setBuilder);

        return setBuilder;
    }

    /// <inheritdoc/>
    public TBuilder Count<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        _propertyExpressions.Add(new CountBuilder<TModel, TEvent, TProperty>(modelPropertyAccessor.GetPropertyPath()));
        return (this as TBuilder)!;
    }
}
