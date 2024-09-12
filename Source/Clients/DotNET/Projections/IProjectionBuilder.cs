// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the based builder for building out projections.
/// </summary>
/// <typeparam name="TModel">Type of model.</typeparam>
/// <typeparam name="TBuilder">Type of actual builder.</typeparam>
public interface IProjectionBuilder<TModel, TBuilder>
    where TBuilder : class
{
    /// <summary>
    /// Automatically map event properties to model properties on the events added.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    IProjectionBuilder<TModel, TBuilder> AutoMap();

    /// <summary>
    /// Sets the initial values to use for a new model instance.
    /// </summary>
    /// <param name="initialValueProviderCallback">Callback for building.</param>
    /// <returns>Builder continuation.</returns>
    /// <remarks>
    /// If one does not provide initial values, the projection engine will leave properties
    /// out that hasn't been met by an event projection expression. This will effectively render
    /// the properties null and might not be desirable when reading instances of the models.
    /// </remarks>
    TBuilder WithInitialValues(Func<TModel> initialValueProviderCallback);

    /// <summary>
    /// Start building the from expressions for a specific event type.
    /// </summary>
    /// <param name="builderCallback">Optional callback for building.</param>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <returns>Builder continuation.</returns>
    /// <remarks>
    /// If using .AutoMap() the properties will be automatically mapped.
    /// In many cases you then don't need to provide a builder callback.
    /// You can override the mapping by providing a builder callback.
    /// </remarks>
    TBuilder From<TEvent>(Action<IFromBuilder<TModel, TEvent>>? builderCallback = default);

    /// <summary>
    /// Start building a join expressions for a specific event type.
    /// </summary>
    /// <param name="builderCallback">Optional callback for building.</param>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <returns>Builder continuation.</returns>
    /// <remarks>
    /// If using .AutoMap() the properties will be automatically mapped.
    /// In many cases you then don't need to provide a builder callback.
    /// You can override the mapping by providing a builder callback.
    /// </remarks>
    TBuilder Join<TEvent>(Action<IJoinBuilder<TModel, TEvent>>? builderCallback = default);

    /// <summary>
    /// Start building property expressions that applies for every events being projected from.
    /// </summary>
    /// <param name="builderCallback">Callback for building.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder FromEvery(Action<IFromEveryBuilder<TModel>> builderCallback);

    /// <summary>
    /// Defines what event removes a child. This is optional, your system can chose to not support removal.
    /// </summary>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <param name="builderCallback">Optional callback for building.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder RemovedWith<TEvent>(Action<RemovedWithBuilder<TModel, TEvent>>? builderCallback = default);

    /// <summary>
    /// Defines what event removes a child through a join. This is optional, your system can chose to not support removal.
    /// </summary>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <param name="builderCallback">Optional callback for building.</param>
    /// <returns>Builder continuation.</returns>
    TBuilder RemovedWithJoin<TEvent>(Action<RemovedWithJoinBuilder<TModel, TEvent>>? builderCallback = default);

    /// <summary>
    /// Start building the children projection for a specific child model.
    /// </summary>
    /// <param name="targetProperty">Expression for expressing the target property.</param>
    /// <param name="builderCallback">Builder callback.</param>
    /// <typeparam name="TChildModel">Type of nested child model.</typeparam>
    /// <returns>Builder continuation.</returns>
    TBuilder Children<TChildModel>(Expression<Func<TModel, IEnumerable<TChildModel>>> targetProperty, Action<IChildrenBuilder<TModel, TChildModel>> builderCallback);
}
