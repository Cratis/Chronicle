// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for joins within children; the join key is the children's IdentifiedBy().
/// </summary>
/// <typeparam name="TReadModel">Child read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IChildrenJoinBuilder<TReadModel, TEvent> : IReadModelPropertiesBuilder<TReadModel, TEvent, IChildrenJoinBuilder<TReadModel, TEvent>>;
