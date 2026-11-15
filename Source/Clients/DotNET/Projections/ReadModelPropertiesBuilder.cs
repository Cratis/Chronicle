// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Projections.ModelBound;
using Cratis.Chronicle.Properties;
using Cratis.Reflection;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IReadModelPropertiesBuilder{TReadModel, TEvent, TBuilder}"/>.
/// </summary>
/// <param name="projectionBuilder">The parent <see cref="IProjectionBuilderFor{TReadModel}"/>.</param>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
/// <typeparam name="TParentBuilder">The type of parent builder.</typeparam>
public class ReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder, TParentBuilder>(IProjectionBuilder<TReadModel, TParentBuilder> projectionBuilder, INamingPolicy namingPolicy)
    : KeyAndParentKeyBuilder<TEvent, TBuilder>(namingPolicy), IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>
        where TBuilder : class, IReadModelPropertiesBuilder<TReadModel, TEvent, TBuilder>
        where TParentBuilder : class
{
#pragma warning disable CA1051 // Visible instance fields
#pragma warning disable SA1600 // Elements should be documented
#pragma warning disable SA1629, CA1002, MA0016 // Return abstract
    protected readonly Dictionary<PropertyPath, IPropertyExpressionBuilder> _propertyExpressions = [];
#pragma warning restore CA1629, CA1002, MA0016 // Return abstract
#pragma warning restore CA1600 // Elements should be documented
#pragma warning restore CA1051 // Visible instance fields
    readonly INamingPolicy _namingPolicy = namingPolicy;

    /// <inheritdoc/>
    public TBuilder Increment<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        _propertyExpressions[propertyPath] = new IncrementBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Decrement<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        _propertyExpressions[propertyPath] = new DecrementBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public IAddBuilder<TReadModel, TEvent, TProperty, TBuilder> Add<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var addBuilder = new AddBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy);
        _propertyExpressions[propertyPath] = addBuilder;
        return addBuilder;
    }

    /// <inheritdoc/>
    public ISubtractBuilder<TReadModel, TEvent, TProperty, TBuilder> Subtract<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var subtractBuilder = new SubtractBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy);
        _propertyExpressions[propertyPath] = subtractBuilder;
        return subtractBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TBuilder> Set(PropertyPath propertyPath)
    {
        var propertyInfo = propertyPath.GetPropertyInfoFor<TReadModel>();
        var primitive = propertyInfo.PropertyType.IsAPrimitiveType() || propertyInfo.PropertyType.IsConcept();
        propertyPath = propertyPath.Segments.Count() == 1
            ? ProjectionPropertyPaths.Resolve(propertyInfo, _namingPolicy, ProjectionPathRole.Target)
            : ProjectionPropertyPaths.Resolve(propertyPath, _namingPolicy, ProjectionPathRole.Target);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var setBuilder = new SetBuilder<TReadModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TProperty, TBuilder> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var targetType = typeof(TProperty);
        var primitive = targetType.IsAPrimitiveType() || targetType.IsConcept();

        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var setBuilder = new SetBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, !primitive);
        _propertyExpressions[propertyPath] = setBuilder;

        return setBuilder;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TBuilder> SetThisValue()
    {
        var propertyPath = ProjectionPropertyPaths.Resolve(WellKnownExpressions.This, _namingPolicy, ProjectionPathRole.Target);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var setBuilder = new SetBuilder<TReadModel, TEvent, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, false);
        _propertyExpressions[propertyPath] = setBuilder;
        return setBuilder;
    }

    /// <inheritdoc/>
    public TBuilder Clear<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);

        // The same ruling the model-bound attributes are held to, enforced in the same place - where the definition
        // is built. C# cannot express "a nullable-annotated reference type" as a generic constraint, so the
        // signature alone cannot refuse this and the check has to be made here.
        ScalarClear.ThrowIfCannotHoldNull(typeof(TReadModel), readModelPropertyAccessor.GetPropertyInfo());

        ThrowIfPropertyAlreadyDefined(propertyPath);
        _propertyExpressions[propertyPath] = new ClearBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder Count<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ResolveTargetPropertyPath(readModelPropertyAccessor);
        ThrowIfPropertyAlreadyDefined(propertyPath);
        _propertyExpressions[propertyPath] = new CountBuilder<TReadModel, TEvent, TProperty>(propertyPath);
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public TBuilder AddChild<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Expression<Func<TEvent, TChildModel>> eventProperty)
    {
        var sourcePropertyInfo = eventProperty.GetPropertyInfo();
        if (sourcePropertyInfo.PropertyType.IsAPrimitiveType())
        {
            projectionBuilder
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
    public TBuilder AddChild<TChildModel>(Expression<Func<TReadModel, IEnumerable<TChildModel>>> targetProperty, Action<IAddChildBuilder<TChildModel, TEvent>> builderCallback)
    {
        projectionBuilder.Children(targetProperty, childrenBuilder =>
        {
            childrenBuilder.From<TEvent>(fromBuilder =>
            {
                var builder = new AddChildBuilder<TReadModel, TChildModel, TEvent>(childrenBuilder, fromBuilder, _namingPolicy);
                builderCallback(builder);
            });
        });
        return (this as TBuilder)!;
    }

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TProperty, TBuilder> SetOnce<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor) =>
        AddSetOnce<TProperty>(ResolveTargetPropertyPath(readModelPropertyAccessor));

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TValue, TBuilder> Set<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddSet<TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TValue, TBuilder> Set<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddSet<TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TValue, TBuilder> SetOnce<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddSetOnce<TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public ISetBuilder<TReadModel, TEvent, TValue, TBuilder> SetOnce<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddSetOnce<TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public TBuilder RemoveKey<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddOperation(new RemoveKeyBuilder(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder RemoveKey<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddOperation(new RemoveKeyBuilder(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder Increment<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddOperation(new IncrementBuilder<TReadModel, TEvent, TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder Increment<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddOperation(new IncrementBuilder<TReadModel, TEvent, TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder Decrement<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddOperation(new DecrementBuilder<TReadModel, TEvent, TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder Decrement<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddOperation(new DecrementBuilder<TReadModel, TEvent, TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder Count<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddOperation(new CountBuilder<TReadModel, TEvent, TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public TBuilder Count<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddOperation(new CountBuilder<TReadModel, TEvent, TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor)));

    /// <inheritdoc/>
    public IAddBuilder<TReadModel, TEvent, TValue, TBuilder> Add<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddAdd<TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public IAddBuilder<TReadModel, TEvent, TValue, TBuilder> Add<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddAdd<TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public ISubtractBuilder<TReadModel, TEvent, TValue, TBuilder> Subtract<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor) =>
        AddSubtract<TValue>(ResolveDictionaryEntryFromEvent(readModelPropertyAccessor, keyAccessor));

    /// <inheritdoc/>
    public ISubtractBuilder<TReadModel, TEvent, TValue, TBuilder> Subtract<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor) =>
        AddSubtract<TValue>(ResolveDictionaryEntryFromContext(readModelPropertyAccessor, keyAccessor));

    static bool IsPrimitive(Type type) => type.IsAPrimitiveType() || type.IsConcept();

    SetBuilder<TReadModel, TEvent, TProperty, TBuilder> AddSet<TProperty>(PropertyPath propertyPath)
    {
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var setBuilder = new SetBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, !IsPrimitive(typeof(TProperty)));
        _propertyExpressions[propertyPath] = setBuilder;
        return setBuilder;
    }

    SetOnceBuilder<TReadModel, TEvent, TProperty, TBuilder> AddSetOnce<TProperty>(PropertyPath propertyPath)
    {
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var setBuilder = new SetOnceBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy, !IsPrimitive(typeof(TProperty)));
        _propertyExpressions[propertyPath] = setBuilder;
        return setBuilder;
    }

    AddBuilder<TReadModel, TEvent, TProperty, TBuilder> AddAdd<TProperty>(PropertyPath propertyPath)
    {
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var addBuilder = new AddBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy);
        _propertyExpressions[propertyPath] = addBuilder;
        return addBuilder;
    }

    SubtractBuilder<TReadModel, TEvent, TProperty, TBuilder> AddSubtract<TProperty>(PropertyPath propertyPath)
    {
        ThrowIfPropertyAlreadyDefined(propertyPath);
        var subtractBuilder = new SubtractBuilder<TReadModel, TEvent, TProperty, TBuilder>((this as TBuilder)!, propertyPath, _namingPolicy);
        _propertyExpressions[propertyPath] = subtractBuilder;
        return subtractBuilder;
    }

    TBuilder AddOperation(IPropertyExpressionBuilder operation)
    {
        ThrowIfPropertyAlreadyDefined(operation.TargetProperty);
        _propertyExpressions[operation.TargetProperty] = operation;
        return (this as TBuilder)!;
    }

    PropertyPath ResolveDictionaryEntryFromEvent<TKey, TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<TEvent, TKey>> keyAccessor)
    {
        var keyPath = ProjectionPropertyPaths.FromExpression(keyAccessor, _namingPolicy, ProjectionPathRole.Source, $"the dictionary key from event '{typeof(TEvent).FullName}' for read model '{typeof(TReadModel).FullName}'");
        return DictionaryEntry(readModelPropertyAccessor, WellKnownExpressions.EventContent, keyPath);
    }

    PropertyPath ResolveDictionaryEntryFromContext<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> keyAccessor)
    {
        var keyPath = ProjectionPropertyPaths.FromExpression(keyAccessor, _namingPolicy, ProjectionPathRole.Source, $"the dictionary key from the event context for read model '{typeof(TReadModel).FullName}'");
        return DictionaryEntry(readModelPropertyAccessor, WellKnownExpressions.EventContext, keyPath);
    }

    PropertyPath DictionaryEntry<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, string keySource, PropertyPath keyPath)
    {
        var dictionaryProperty = ProjectionPropertyPaths.FromExpression(readModelPropertyAccessor, _namingPolicy, ProjectionPathRole.Target, $"the dictionary-typed property on read model '{typeof(TReadModel).FullName}'");
        return ProjectionPropertyPaths.DictionaryEntry(dictionaryProperty, keySource, keyPath);
    }

    PropertyPath ResolveTargetPropertyPath<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor) =>
        ProjectionPropertyPaths.FromExpression(readModelPropertyAccessor, _namingPolicy, ProjectionPathRole.Target, $"the target property on read model '{typeof(TReadModel).FullName}'");

    void ThrowIfPropertyAlreadyDefined(PropertyPath propertyPath)
    {
        if (_propertyExpressions.ContainsKey(propertyPath))
        {
            throw new DuplicatePropertyInProjection(propertyPath);
        }
    }
}
