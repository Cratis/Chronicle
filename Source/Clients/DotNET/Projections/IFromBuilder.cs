// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building from expressions.
/// </summary>
/// <typeparam name="TReadModel">Read model to build for.</typeparam>
/// <typeparam name="TEvent">Event to build for.</typeparam>
public interface IFromBuilder<TReadModel, TEvent> : IReadModelPropertiesBuilder<TReadModel, TEvent, IFromBuilder<TReadModel, TEvent>>;
