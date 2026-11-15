// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines how the observers filling a read model are replayed into a sink the read model moved to.
/// </summary>
/// <remarks>
/// Replaying is split in two so it survives a crash: <see cref="Request"/> records a durable replay recommendation for every
/// observer before the new sink is stored, and <see cref="Start"/> performs them once it is, when the definition evolution
/// policy allows. A crash in between leaves the recommendations standing, so the new sink is never silently left empty.
/// </remarks>
public interface ISinkChangeReplays
{
    /// <summary>
    /// Record a replay recommendation for every projection and reducer filling a read model, in every namespace.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> whose sink changes.</param>
    /// <returns>The <see cref="SinkChangeReplay"/> to start once the new sink is stored.</returns>
    /// <remarks>Requesting again for the same observers returns the recommendations already recorded.</remarks>
    Task<SinkChangeReplay> Request(EventStoreName eventStore, ReadModelIdentifier readModel);

    /// <summary>
    /// Perform the recorded replays when the definition evolution policy allows a full replay. A replay that is refused stays recommended.
    /// </summary>
    /// <param name="replay">The <see cref="SinkChangeReplay"/> from <see cref="Request"/>.</param>
    /// <returns>Awaitable task.</returns>
    Task Start(SinkChangeReplay replay);
}
