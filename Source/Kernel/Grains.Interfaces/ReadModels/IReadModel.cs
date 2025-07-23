// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Grains.ReadModels;

/// <summary>
/// Defines a read model.
/// </summary>
public interface IReadModel : IGrainWithStringKey
{
    /// <summary>
    /// Set the read model definition and subscribe as an observer.
    /// </summary>
    /// <param name="definition"><see cref="ReadModelDefinition"/> to refresh with.</param>
    /// <returns>Awaitable task.</returns>
    Task SetDefinition(ReadModelDefinition definition);

    /// <summary>
    /// Get the read model definition.
    /// </summary>
    /// <returns>The <see cref="ReadModelDefinition"/>.</returns>
    Task<ReadModelDefinition> GetDefinition();
}
