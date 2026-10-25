// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Projections.Generations;

namespace Cratis.Chronicle.Storage.Generations.Bindings;

/// <summary>
/// Defines conditional writer admission for durable generation owners.
/// </summary>
/// <remarks>
/// Resolve this capability from the authoritative binding storage. Participants receive the returned fixed grant;
/// they must not acquire a replacement themselves. Zero is the expected epoch of a never-admitted generation.
/// A failed comparison returns null without changing the gate. After an unknown outcome, an owner must recover
/// authority rather than retry against a freshly read epoch.
/// </remarks>
public interface IGenerationWriterGrants
{
    /// <summary>
    /// Issues a grant only if the stored execution binding and expected gate epoch still match.
    /// </summary>
    /// <param name="store">The event store.</param>
    /// <param name="binding">The explicitly resolved immutable binding.</param>
    /// <param name="attempt">The owner attempt.</param>
    /// <param name="expectedEpoch">The epoch authorized by the owner's transition.</param>
    /// <returns>The new fixed grant, or null on a stale transition.</returns>
    /// <exception cref="MaterializationBindingsNotSupported">This provider cannot admit generation writers.</exception>
    Task<GenerationWriterGrant?> TryAcquire(EventStoreName store, GenerationObserverBinding binding, GenerationBuildOperationId attempt, long expectedEpoch) =>
        throw new MaterializationBindingsNotSupported();
}
