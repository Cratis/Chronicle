// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Cuts;
using Cratis.Chronicle.Concepts.ReadModels;

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// Represents a request to capture a selection of read models exactly at a vector of event-sequence cuts.
/// </summary>
/// <param name="EventStore">The event store the selection belongs to.</param>
/// <param name="Namespace">The namespace the selection belongs to.</param>
/// <param name="Cuts">The exact position, per event sequence, every selected read model is bound to.</param>
/// <param name="Selection">The read model identifiers to capture.</param>
public sealed record ReadModelCutRequest(
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    IReadOnlyCollection<EventSequenceCut> Cuts,
    IReadOnlyCollection<ReadModelIdentifier> Selection);
