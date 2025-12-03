// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections;

/// <summary>
/// Defines a system that can define a projection.
/// </summary>
/// <typeparam name="TReadModel">Model type to target.</typeparam>
public interface IProjectionFor<TReadModel> : IProjection
    where TReadModel : class
{
    /// <summary>
    /// Defines the projection.
    /// </summary>
    /// <param name="builder"><see cref="IProjectionBuilderFor{TReadModel}"/> to use for building the definition.</param>
    void Define(IProjectionBuilderFor<TReadModel> builder);
}
