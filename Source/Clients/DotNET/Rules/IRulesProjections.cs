// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Chronicle.Contracts.Projections;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Chronicle.Rules;

/// <summary>
/// Defines a system that knows about projections related to rules.
/// </summary>
internal interface IRulesProjections
{
    /// <summary>
    /// Gets all read models that has business rules defined.
    /// </summary>
    IEnumerable<IHaveReadModel> ReadModels { get; }

    /// <summary>
    /// Discover all the projection definitions related to rules.
    /// </summary>
    /// <returns><see cref="IImmutableList{T}"/> of <see cref="ProjectionDefinition"/>.</returns>
    IImmutableList<ProjectionDefinition> Discover();
}
