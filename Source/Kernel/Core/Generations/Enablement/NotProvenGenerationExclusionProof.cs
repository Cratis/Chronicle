// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Refuses enablement until an operator supplies a real exclusion implementation.
/// </summary>
public sealed class NotProvenGenerationExclusionProof : IGenerationExclusionProof
{
    /// <inheritdoc/>
    public Task<bool> IsProven(EventStoreName eventStore) => Task.FromResult(false);
}
