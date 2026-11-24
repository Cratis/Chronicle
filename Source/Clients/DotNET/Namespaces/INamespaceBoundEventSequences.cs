// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.EventSequences;

namespace Cratis.Chronicle.Namespaces;

/// <summary>An opt-in writer factory, separate from the existing event store interface.</summary>
public interface INamespaceBoundEventSequences
{
    /// <summary>Creates a writer pinned to a snapshot captured before decision reads. Never refreshes the snapshot.</summary>
    /// <param name="id">The event sequence.</param>
    /// <param name="binding">An open binding for this event store and namespace.</param>
    /// <returns>A writer that uses only namespace-protected append routes.</returns>
    IEventSequence GetEventSequence(EventSequenceId id, NamespaceBinding binding);
}
