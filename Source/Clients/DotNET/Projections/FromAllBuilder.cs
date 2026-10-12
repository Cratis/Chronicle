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
/// includes child events in their containing parent's mappings and also observes unrelated event types.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
/// <param name="namingPolicy">The <see cref="INamingPolicy"/> to use for converting names during serialization.</param>
public class FromAllBuilder<TReadModel>(INamingPolicy namingPolicy) : IFromAllBuilder<TReadModel>
{
    readonly List<IPropertyExpressionBuilder> _propertyExpressions = [];

    /// <inheritdoc/>
    public IAllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor)
    {
        var propertyPath = ProjectionPropertyPaths.FromExpression(readModelPropertyAccessor, namingPolicy, ProjectionPathRole.Target, $"the property to set on read model '{typeof(TReadModel).FullName}'");
        var setBuilder = new AllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>>(this, propertyPath, namingPolicy);
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

    PropertyPath GetTargetProperty<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor, string description) =>
        ProjectionPropertyPaths.FromExpression(readModelPropertyAccessor, namingPolicy, ProjectionPathRole.Target, $"{description} read model '{typeof(TReadModel).FullName}'");

    PropertyPath BuildDynamicTargetProperty<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor)
    {
        var dictionaryProperty = ProjectionPropertyPaths.FromExpression(readModelPropertyAccessor, namingPolicy, ProjectionPathRole.Target, $"the dictionary-typed property on read model '{typeof(TReadModel).FullName}'");
        var contextProperty = ProjectionPropertyPaths.FromExpression(dynamicKeyAccessor, namingPolicy, ProjectionPathRole.Source, $"the dynamic dictionary key for read model '{typeof(TReadModel).FullName}'");

        // Compose already-converted paths with the well-known context marker, not CLR member names.
        // The context suffix is a source expression for a dynamic dictionary key, not a target member.
        return new PropertyPath($"{dictionaryProperty}.{WellKnownExpressions.EventContext}.{contextProperty}");
    }
}
