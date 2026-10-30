// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Generations.Enablement;

/// <summary>
/// Proves external exclusion of old writers, independently of membership.
/// </summary>
public interface IGenerationExclusionProof
{
    /// <summary>
    /// Checks exclusion for this store.
    /// </summary>
    /// <param name="eventStore">The store.</param>
    /// <returns>Whether exclusion is proven.</returns>
    Task<bool> IsProven(EventStoreName eventStore);
}
