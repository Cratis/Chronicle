// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations.Enablement;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Serializes generation initialization per store; deliberately not reentrant.
/// </summary>
public interface IGenerationInitializer : IGrainWithStringKey
{
    /// <summary>
    /// Initializes the named store without changing its physical data.
    /// </summary>
    /// <param name="eventStore">The store matching the grain key.</param>
    /// <returns>The durable outcome.</returns>
    Task<GenerationInitializationResult> Initialize(EventStoreName eventStore);
}
