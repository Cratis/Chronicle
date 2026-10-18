// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a builder for building set operations for properties that will be applied for all events the projection is projecting from - represented as expressions.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TParentBuilder">Type of the parent builder.</typeparam>
public interface IAllSetBuilder<TReadModel, TParentBuilder> : IPropertyExpressionBuilder
{
    /// <summary>
    /// Map to a constant value for every matching event.
    /// </summary>
    /// <typeparam name="TProperty">Type of the constant value.</typeparam>
    /// <param name="value">The value to set, or null to clear the property.</param>
    /// <returns>Builder continuation.</returns>
    /// <remarks>
    /// The value is not type-checked against the target read model property, because this builder does not carry the
    /// property type - <typeparamref name="TProperty"/> is inferred from <paramref name="value"/>. Pass a value whose type
    /// matches the property; a mismatch is not reported when the projection is defined.
    /// </remarks>
    TParentBuilder ToValue<TProperty>(TProperty value);

    /// <summary>
    /// Map to the event source id on the metadata of the event.
    /// </summary>
    /// <returns>Builder continuation.</returns>
    TParentBuilder ToEventSourceId();

    /// <summary>
    /// Map to a property on the <see cref="EventContext"/>.
    /// </summary>
    /// <param name="eventContextPropertyAccessor">Property accessor for specifying which property to map to.</param>
    /// <returns>Builder continuation.</returns>
    TParentBuilder ToEventContextProperty(Expression<Func<EventContext, object>> eventContextPropertyAccessor);
}
