// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Projections;

namespace Cratis.Chronicle.Projections.ModelBound;

/// <summary>
/// Defines a system for discovering and building projection definitions from model-bound attributes.
/// </summary>
internal interface IModelBoundProjections
{
    /// <summary>
    /// Gets the read models that the last <see cref="Discover"/> could not build, with the failure that stopped each one.
    /// </summary>
    /// <remarks>
    /// Discovery isolates a read model that cannot be built so that it costs itself and nothing else, and logs it. That
    /// leaves the read side partially registered with nothing but a log line to say so, which is why the failure is kept
    /// here as well - it is what lets the outcome of registration be asked about rather than only told.
    /// </remarks>
    IDictionary<Type, Exception> Failures { get; }

    /// <summary>
    /// Discovers all model-bound projections.
    /// </summary>
    /// <returns>A collection of <see cref="ProjectionDefinition"/>.</returns>
    IDictionary<Type, ProjectionDefinition> Discover();
}
