// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Cuts;

namespace Cratis.Chronicle.Storage.Cuts;

/// <summary>
/// Represents the immutable, published record of a read-model cut - what was requested, and the outcome for
/// every selected read model.
/// </summary>
/// <param name="Id">The deterministic <see cref="ReadModelCutId"/> for this exact request.</param>
/// <param name="EventStore">The event store the capture belongs to.</param>
/// <param name="Namespace">The namespace the capture belongs to.</param>
/// <param name="Cuts">The exact position, per event sequence, every entry is bound to.</param>
/// <param name="Entries">The outcome for every read model in the requested selection.</param>
/// <param name="PublishedAt">When the manifest was published. Provenance only - never used to decide correctness.</param>
/// <remarks>
/// A manifest is only ever written once every entry's payload (for the entries that succeeded) has been written
/// and verified - there is no partially-published manifest, and nothing reads a capture as valid before this
/// record exists.
/// </remarks>
public sealed record ReadModelCutManifest(
    ReadModelCutId Id,
    EventStoreName EventStore,
    EventStoreNamespaceName Namespace,
    IReadOnlyCollection<EventSequenceCut> Cuts,
    IReadOnlyCollection<ReadModelCutEntry> Entries,
    DateTimeOffset PublishedAt);
