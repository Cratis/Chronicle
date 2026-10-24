// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Generations.Bindings;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Bindings;

/// <summary>Looks up generation ownership of physical containers.</summary>
public interface IGenerationContainerClaims
{
    /// <summary>Finds every claim on a physical collection.</summary>
    /// <param name="collection">The physical collection name.</param>
    /// <returns>The generation claims.</returns>
    Task<IReadOnlyCollection<GenerationContainerClaim>> For(string collection);
}
