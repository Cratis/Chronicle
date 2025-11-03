// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines a system for discovering and building projection definitions from model-bound attributes.
/// </summary>
public interface IModelBoundProjections
{
    /// <summary>
    /// Discovers all model-bound projections.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    IEnumerable<ProjectionDefinition> Discover();
}
