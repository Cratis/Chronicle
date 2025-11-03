// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines a builder for creating projection definitions from model-bound attributes.
/// </summary>
public interface IModelBoundProjectionBuilder
{
    /// <summary>
    /// Builds a projection definition from a type with model-bound projection attributes.
    /// </summary>
    /// <param name="modelType">The type of the read model.</param>
    /// <returns>The <see cref="ProjectionDefinition"/>.</returns>
    ProjectionDefinition Build(Type modelType);
}
