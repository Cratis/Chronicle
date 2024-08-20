// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.Expressions;
using Cratis.Chronicle.Properties;
using Cratis.Reflection;
using Cratis.Strings;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IModelPropertiesBuilder{TModel, TEvent, TBuilder}"/>.
/// </summary>
/// <typeparam name="TModel">Model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
/// <typeparam name="TParentBuilder">The type of parent builder.</typeparam>
public class ModelPropertiesBuilder<TModel, TEvent, TBuilder, TParentBuilder> : IModelPropertiesBuilder<TModel, TEvent, TBuilder>
    where TBuilder : class, IModelPropertiesBuilder<TModel, TEvent, TBuilder>
    where TParentBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly Dictionary<PropertyPath, IPropertyExpressionBuilder> _propertyExpressions = [];
    protected IKeyBuilder _parentKey = new KeyBuilder(new NoExpression());
    protected IKeyBuilder _key = new KeyBuilder(new EventSourceIdExpression());
#pragma warning restore CA1629, CA1002, MA0016 // Return abstract
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields
    readonly IProjectionBuilder<TModel, TParentBuilder> _projectionBuilder;

    /// <summary>
    /// Initializes a new instance of the <see cref="ModelPropertiesBuilder{TModel, TEvent, TBuilder, TParentBuilder}"/>.
    /// </summary>
    /// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TModel}"/>.</param>
    protected ModelPropertiesBuilder(IProjectionBuilder<TModel, TParentBuilder> projectionBuilder)
    {
        _projectionBuilder = projectionBuilder;
    }

    /// <inheritdoc/>
    public TBuilder AutoMap()
    {
        var eventProperties = typeof(TEvent).GetProperties().Select(_ => _.Name.ToCamelCase());
        var modelProperties = typeof(TModel).GetProperties().Select(_ => _.Name.ToCamelCase());

        foreach (var property in eventProperties.Intersect(modelProperties))
        {
            Set(property).To(property);
        }

        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _key = new KeyBuilder(new EventContentPropertyExpression(keyAccessor.GetPropertyPath()));
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentKey<TProperty>(Expression<Func<TEvent, TProperty>> keyAccessor)
    {
        _parentKey = new KeyBuilder(new EventContentPropertyExpression(keyAccessor.GetPropertyPath()));
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback)
    {
        var compositeKeyBuilder = new CompositeKeyBuilder<TKeyType, TEvent>();
        builderCallback(compositeKeyBuilder);
        _parentKey = compositeKeyBuilder;
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingParentKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor)
    {
        _parentKey = new KeyBuilder(new EventContextPropertyExpression(keyAccessor.GetPropertyPath()));
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingKeyFromContext(Expression<Func<TEvent, EventContext>> keyAccessor)
    {
        _parentKey = new KeyBuilder(new EventContextPropertyExpression(keyAccessor.GetPropertyPath()));
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder UsingCompositeKey<TKeyType>(Action<ICompositeKeyBuilder<TKeyType, TEvent>> builderCallback)
    {
        var compositeKeyBuilder = new CompositeKeyBuilder<TKeyType, TEvent>();
        builderCallback(compositeKeyBuilder);
        _key = compositeKeyBuilder;
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public IAddBuilder<TModel, TEvent, TProperty, TBuilder> Add<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        var addBuilder = new AddBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath);
        _propertyExpressions[propertyPath] = addBuilder;
        return addBuilder;
    }

    /// <inheritdoc/>
    public ISubtractBuilder<TModel, TEvent, TProperty, TBuilder> Subtract<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        var subtractBuilder = new SubtractBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath);
        _propertyExpressions[propertyPath] = subtractBuilder;
        return subtractBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TBuilder> Set(PropertyPath propertyPath)
    {
        var propertyInfo = propertyPath.GetPropertyInfoFor<TModel>();
        var primitive = propertyInfo.PropertyType.IsAPrimitiveType() || propertyInfo.PropertyType.IsConcept();
        var setBuilder = new SetBuilder<TModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        var setBuilder = new SetBuilder<TModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TModel, TEvent, TBuilder> SetThisValue()
    {
        var propertyPath = new PropertyPath("$this");
        var setBuilder = new SetBuilder<TModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, false);
        _propertyExpressions[propertyPath] = setBuilder;
        return setBuilder;
    }

    /// <inheritdoc/>
    public TBuilder Count<TProperty>(Expression<Func<TModel, TProperty>> modelPropertyAccessor)
    {
        var propertyPath = modelPropertyAccessor.GetPropertyPath();
        _propertyExpressions[propertyPath] = new CountBuilder<TModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder AddChild<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Expression<Func<TEvent, TChildModel>> eventProperty)
    {
        var sourcePropertyInfo = eventProperty.GetPropertyInfo();
        if (sourcePropertyInfo.PropertyType.IsAPrimitiveType())
        {
            _projectionBuilder
                .Children(targetProperty, childrenBuilder => childrenBuilder
                    .FromEventProperty(eventProperty));
        }
        else
        {
            AddChild(targetProperty, builder => builder.FromObject(eventProperty));
        }

        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder AddChild<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IAddChildBuilder<TChildModel, TEvent>> builderCallback)
    {
        _projectionBuilder.Children(targetProperty, childrenBuilder =>
        {
            childrenBuilder.From<TEvent>(fromBuilder =>
            {
                var builder = new AddChildBuilder<TModel, TChildModel, TEvent>(childrenBuilder, fromBuilder);
                builderCallback(builder);
            });
        });
        return (this as TBuilder)!;
    }
}
