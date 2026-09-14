// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building properties that can be set by all event types.
/// </summary>
/// <typeparam name="TReadModel">Type of read model to build for.</typeparam>
public interface IFromAllBuilder<TReadModel>
{
    /// <summary>
    /// Start building the set operation to a target property on the read model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for defining the target property.</param>
    /// <returns>The <see cref="IAllSetBuilder{TReadModel, TBuilder}"/> to build up the property expressions.</returns>
    IAllSetBuilder<TReadModel, IFromAllBuilder<TReadModel>> Set<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor);

    /// <summary>
    /// Count every event towards a property on the read model.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    IFromAllBuilder<TReadModel> Count<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor);

    /// <summary>
    /// Count every event towards a key in a dictionary-typed property on the read model, where the key is
    /// resolved dynamically, per event, from the <see cref="EventContext"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for the dictionary-typed target property.</param>
    /// <param name="dynamicKeyAccessor">Accessor for the <see cref="EventContext"/> property to derive the dictionary key from.</param>
    /// <returns>Builder continuation.</returns>
    IFromAllBuilder<TReadModel> Count<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor);

    /// <summary>
    /// Increment a property on the read model for every event.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    IFromAllBuilder<TReadModel> Increment<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor);

    /// <summary>
    /// Increment a key in a dictionary-typed property on the read model, where the key is resolved dynamically,
    /// per event, from the <see cref="EventContext"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for the dictionary-typed target property.</param>
    /// <param name="dynamicKeyAccessor">Accessor for the <see cref="EventContext"/> property to derive the dictionary key from.</param>
    /// <returns>Builder continuation.</returns>
    IFromAllBuilder<TReadModel> Increment<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor);

    /// <summary>
    /// Decrement a property on the read model for every event.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for defining the target property.</param>
    /// <returns>Builder continuation.</returns>
    IFromAllBuilder<TReadModel> Decrement<TProperty>(Expression<Func<TReadModel, TProperty>> readModelPropertyAccessor);

    /// <summary>
    /// Decrement a key in a dictionary-typed property on the read model, where the key is resolved dynamically,
    /// per event, from the <see cref="EventContext"/>.
    /// </summary>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="readModelPropertyAccessor">Read model property accessor for the dictionary-typed target property.</param>
    /// <param name="dynamicKeyAccessor">Accessor for the <see cref="EventContext"/> property to derive the dictionary key from.</param>
    /// <returns>Builder continuation.</returns>
    IFromAllBuilder<TReadModel> Decrement<TValue>(Expression<Func<TReadModel, IDictionary<string, TValue>>> readModelPropertyAccessor, Expression<Func<EventContext, object>> dynamicKeyAccessor);
}
