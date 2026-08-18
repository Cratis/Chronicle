// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Sinks;

/// <summary>
/// Defines what a storage implementation has to supply for the shared <see cref="ISink"/> contract
/// specifications to run against it.
/// </summary>
/// <remarks>
/// The sink contract is written once and executed against every implementation, because a signature
/// says nothing about semantics: the implementations are only interchangeable if they behave the
/// same, and a divergence between them does not fail - it makes specifications pass vacuously.
/// </remarks>
public interface ISinkHarness : IDisposable
{
    /// <summary>
    /// Creates the <see cref="ISink"/> under specification for the given read model.
    /// </summary>
    /// <param name="definition">The <see cref="ReadModelDefinition"/> the sink writes.</param>
    /// <returns>The <see cref="ISink"/> to run the contract against.</returns>
    ISink CreateSink(ReadModelDefinition definition);
}
