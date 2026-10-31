// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Occurrences;

namespace Cratis.Chronicle.Occurrences;

/// <summary>
/// Coordinates lifecycle intents for one event store; storage decides uniqueness.
/// </summary>
public interface ILifecycleOccurrences : IGrainWithStringKey
{
    /// <summary>
    /// Records a logical lifecycle occurrence or durable namespace fan-out intent.
    /// </summary>
    /// <param name="kind">Startup or shutdown.</param>
    /// <param name="activationId">The host-supplied activation.</param>
    /// <param name="allNamespaces">Whether to snapshot all namespaces.</param>
    /// <returns>The serialized durable result.</returns>
    Task<string> Record(OccurrenceKind kind, string activationId, bool allNamespaces);

    /// <summary>
    /// Restores the reconciliation timer and completes one bounded page of pending intents.
    /// </summary>
    /// <returns>The reconciliation work.</returns>
    Task Ensure();
}
