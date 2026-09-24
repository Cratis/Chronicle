// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;
using Cratis.Chronicle.Concepts.Projections;

namespace Cratis.Chronicle.Projections.Kernel;

/// <summary>
/// Defines the builder for a system projection.
/// </summary>
/// <typeparam name="TReadModel">Type of the read model the projection materializes.</typeparam>
public interface IKernelProjectionBuilder<TReadModel>
    where TReadModel : class
{
    /// <summary>
    /// Set the <see cref="ProjectionScope"/> the projection materializes its read model in.
    /// </summary>
    /// <param name="scope"><see cref="ProjectionScope"/> to use.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// Defaults to <see cref="ProjectionScope.Namespaced"/>. Choose <see cref="ProjectionScope.Both"/> when the
    /// same figure is wanted for the server as a whole and per namespace - the global instance says a number is
    /// wrong and the namespaced instances say where.
    /// </remarks>
    IKernelProjectionBuilder<TReadModel> ScopedTo(ProjectionScope scope);

    /// <summary>
    /// Set the event sequence the projection observes.
    /// </summary>
    /// <param name="eventSequenceId"><see cref="EventSequenceId"/> to observe.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionBuilder<TReadModel> FromEventSequence(EventSequenceId eventSequenceId);

    /// <summary>
    /// Identify the read model instance by a single expression.
    /// </summary>
    /// <param name="expression">The key expression.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionBuilder<TReadModel> IdentifiedBy(PropertyExpression expression);

    /// <summary>
    /// Identify the read model instance by a composite of several expressions.
    /// </summary>
    /// <param name="builderCallback">Callback for building the composite key.</param>
    /// <returns>The builder for continuation.</returns>
    /// <remarks>
    /// This is what turns a projection into a grouping. A composite of event type and namespace, for instance,
    /// yields one read model instance per event type per namespace - the distribution a dashboard needs, computed
    /// as events arrive rather than by scanning the store when someone opens a page.
    /// </remarks>
    IKernelProjectionBuilder<TReadModel> IdentifiedByComposite(Action<IKernelProjectionCompositeKeyBuilder<TReadModel>> builderCallback);

    /// <summary>
    /// Build from a specific <see cref="EventType"/>.
    /// </summary>
    /// <param name="eventType"><see cref="EventType"/> to build from.</param>
    /// <param name="builderCallback">Callback for building what the event contributes.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionBuilder<TReadModel> From(EventType eventType, Action<IKernelProjectionFromBuilder<TReadModel>> builderCallback);

    /// <summary>
    /// Build from every event reaching the observed event sequence, whatever its type.
    /// </summary>
    /// <param name="builderCallback">Callback for building what every event contributes.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionBuilder<TReadModel> FromEvery(Action<IKernelProjectionFromBuilder<TReadModel>> builderCallback);

    /// <summary>
    /// Set the initial value of a read model property for a newly created instance.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="propertyAccessor">Accessor for the property to set.</param>
    /// <param name="value">The initial value.</param>
    /// <returns>The builder for continuation.</returns>
    IKernelProjectionBuilder<TReadModel> WithInitialValue<TProperty>(Expression<Func<TReadModel, TProperty>> propertyAccessor, TProperty value);
}
