// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// A fixed writer admission issued by a conditional owner transition, never by sink construction.
/// </summary>
/// <param name="Store">The event store.</param>
/// <param name="Binding">The immutable execution binding.</param>
/// <param name="Attempt">The owner attempt sharing this grant with its participants.</param>
/// <param name="Epoch">The admitted gate epoch.</param>
public sealed record GenerationWriterGrant(EventStoreName Store, GenerationObserverBinding Binding, GenerationBuildOperationId Attempt, long Epoch)
{
    /// <summary>
    /// Gets the identity publication revokes in the same database as the destination.
    /// </summary>
    public GenerationGateId Gate => new(Binding.Key, Binding.Identity.Generation);
}
