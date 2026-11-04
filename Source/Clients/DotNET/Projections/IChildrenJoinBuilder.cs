// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq.Expressions;

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for joins within children; the join key is the children's IdentifiedBy().
/// </summary>
/// <typeparam name="TReadModel">Child read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IChildrenJoinBuilder<TReadModel, TEvent> : IReadModelPropertiesBuilder<TReadModel, TEvent, IChildrenJoinBuilder<TReadModel, TEvent>>
{
    /// <summary>
    /// Sets an explicit join property, which is not supported for child joins.
    /// </summary>
    /// <typeparam name="TProperty">Type of the property.</typeparam>
    /// <param name="keyAccessor">Accessor for the property to use.</param>
    /// <returns>Builder continuation.</returns>
    [Obsolete("Child joins always join on the identity set with IdentifiedBy() on the children builder, and specifying On() throws OnPropertyShouldNotBeSpecifiedForChildJoin when the projection is built. Remove the call - On() on child joins will be removed in the next major version.")]
    IChildrenJoinBuilder<TReadModel, TEvent> On<TProperty>(Expression<Func<TReadModel, TProperty>> keyAccessor);
}
