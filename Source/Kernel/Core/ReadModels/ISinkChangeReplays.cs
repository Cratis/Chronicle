// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.ReadModels;

/// <summary>
/// Defines how the observers filling a read model are replayed into a sink the read model moved to.
/// </summary>
public interface ISinkChangeReplays
{
    /// <summary>
    /// Replay every projection and reducer filling a read model, in every namespace, as a definition change does.
    /// </summary>
    /// <param name="eventStore">The <see cref="EventStoreName"/> the read model belongs to.</param>
    /// <param name="readModel">The <see cref="ReadModelIdentifier"/> whose sink changed.</param>
    /// <returns>Awaitable task.</returns>
    Task ReplayFor(EventStoreName eventStore, ReadModelIdentifier readModel);
}
