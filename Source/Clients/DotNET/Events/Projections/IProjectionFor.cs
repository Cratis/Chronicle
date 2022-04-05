// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Aksio.Cratis.Events.Projections;

/// <summary>
/// Defines something that can define a projection.
/// </summary>
/// <typeparam name="TModel">Model type to target.</typeparam>
public interface IProjectionFor<TModel>
{
    /// <summary>
    /// Gets the unique identifier of the projection.
    /// </summary>
    ProjectionId Identifier { get; }

    /// <summary>
    /// Defines the projection.
    /// </summary>
    /// <param name="builder"><see cref="IProjectionBuilderFor{TModel}"/> to use for building the definition.</param>
    void Define(IProjectionBuilderFor<TModel> builder);
}
