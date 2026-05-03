// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines the builder for building out a nested single-object relationship on a model.
/// </summary>
/// <typeparam name="TParentReadModel">Parent read model type.</typeparam>
/// <typeparam name="TNestedReadModel">Nested read model type.</typeparam>
public interface INestedBuilder<TParentReadModel, TNestedReadModel> : IProjectionBuilder<TNestedReadModel, INestedBuilder<TParentReadModel, TNestedReadModel>>
{
    /// <summary>
    /// Defines what event clears (sets to null) the nested object.
    /// </summary>
    /// <typeparam name="TEvent">Type of event.</typeparam>
    /// <returns>Builder continuation.</returns>
    INestedBuilder<TParentReadModel, TNestedReadModel> ClearWith<TEvent>();
}
