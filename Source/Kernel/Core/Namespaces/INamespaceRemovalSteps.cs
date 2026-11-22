// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Storage.Namespaces;

namespace Cratis.Chronicle.Namespaces;

/// <summary>
/// Executes individual cleanup checkpoints after the lifecycle owner has persisted their intent and closed the marker.
/// This is an internal orchestration seam, not a public removal endpoint or an admission check.
/// </summary>
public interface INamespaceRemovalSteps
{
    /// <summary>Executes exactly one Forget, Cleanup, Drop, or DeleteListing checkpoint.</summary>
    /// <param name="eventStore">The event store.</param>
    /// <param name="namespace">The canonical namespace.</param>
    /// <param name="stage">The persisted removal checkpoint.</param>
    /// <returns>The acknowledged checkpoint completion; exceptions do not prove the provider's terminal outcome.</returns>
    Task Execute(EventStoreName eventStore, EventStoreNamespaceName @namespace, NamespaceRemovalStage stage);
}
