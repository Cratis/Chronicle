// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Properties;
using Cratis.Serialization;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Represents an implementation of <see cref="IFromAllBuilder{TReadModel}"/>. Its all-events subscription
/// observes child event types and unrelated event types; the stored child-inclusion flag is not a filter.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
public class FromAllBuilder<TReadModel>(INamingPolicy namingPolicy) : IFromAllBuilder<TReadModel>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = [];

    /// <inheritdoc/>
    public IAllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        if (!readModelPropertyAccessor.TryGetPropertyPath(out var accessorPropertyPath))
        {
            throw new InvalidPropertyExpression($"the property to set on read model '{typeof(TReadModel).FullName}'", readModelPropertyAccessor);
        }

        var setBuilder = new AllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>>(this, namingPolicy.GetPropertyName(accessorPropertyPath), namingPolicy);
        _propertyExpressions.Add(setBuilder);
        return setBuilder;
    }

    /// <inheritdoc/>
    public IFromAllBuilder<TReadModel> Count<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor) =>
        AddOperation(GetTargetProperty(readModelPropertyAccessor, "the property to count on"), WellKnownExpressions.Count);

    /// <inheritdoc/>
    public IFromAllBuilder<TReadModel> Count<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor) =>
        AddOperation(BuildDynamicTargetProperty(readModelPropertyAccessor, dynamicKeyAccessor), WellKnownExpressions.Count);

    /// <inheritdoc/>
    public IFromAllBuilder<TReadModel> Increment<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor) =>
        AddOperation(GetTargetProperty(readModelPropertyAccessor, "the property to increment on"), WellKnownExpressions.Increment);

    /// <inheritdoc/>
    public IFromAllBuilder<TReadModel> Increment<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor) =>
        AddOperation(BuildDynamicTargetProperty(readModelPropertyAccessor, dynamicKeyAccessor), WellKnownExpressions.Increment);

    /// <inheritdoc/>
    public IFromAllBuilder<TReadModel> Decrement<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor) =>
        AddOperation(GetTargetProperty(readModelPropertyAccessor, "the property to decrement on"), WellKnownExpressions.Decrement);

    /// <inheritdoc/>
    public IFromAllBuilder<TReadModel> Decrement<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor) =>
        AddOperation(BuildDynamicTargetProperty(readModelPropertyAccessor, dynamicKeyAccessor), WellKnownExpressions.Decrement);

    /// <summary>
    /// Builds a <see cref="FromEveryDefinition"/> from expressions.
    /// </summary>
    /// <returns>A new <see cref="FromEveryDefinition"/> instance.</returns>
    internal FromEveryDefinition Build() => new() { Properties = _propertyExpressions.ToDictionary(_ => (string)_.TargetProperty, _ => _.Build()), IncludeChildren = true };

    FromAllBuilder<TReadModel> AddOperation(PropertyPath targetProperty, string operationExpression)
    {
        _propertyExpressions.Add(new FromAllOperationBuilder(targetProperty, operationExpression));
        return this;
    }

    PropertyPath GetTargetProperty<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor, string description)
    {
        if (!readModelPropertyAccessor.TryGetPropertyPath(out var accessorPropertyPath))
        {
            throw new InvalidPropertyExpression($"{description} read model '{typeof(TReadModel).FullName}'", readModelPropertyAccessor);
        }

        return namingPolicy.GetPropertyName(accessorPropertyPath);
    }

    PropertyPath BuildDynamicTargetProperty<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor)
    {
        if (!readModelPropertyAccessor.TryGetPropertyPath(out var accessorPropertyPath))
        {
            throw new InvalidPropertyExpression($"the dictionary-typed property on read model '{typeof(TReadModel).FullName}'", readModelPropertyAccessor);
        }

        if (!dynamicKeyAccessor.TryGetPropertyPath(out var dynamicKeyPropertyPath))
        {
            throw new InvalidPropertyExpression($"the dynamic dictionary key for read model '{typeof(TReadModel).FullName}'", dynamicKeyAccessor);
        }

        var dictionaryProperty = namingPolicy.GetPropertyName(accessorPropertyPath);
        var contextProperty = namingPolicy.GetPropertyName(dynamicKeyPropertyPath);
        return new PropertyPath($"{dictionaryProperty}.{WellKnownExpressions.EventContext}.{contextProperty}");
    }
}
