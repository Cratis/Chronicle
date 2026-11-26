// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Observation;

/// <summary>
/// Represents the durable membership intent while the observer drains and reconciles its work.
/// Job and partition progress belong to separate rows, not this intent.
/// </summary>
/// <param name="Revision">The revision the change will publish.</param>
/// <param name="Started">When the change began.</param>
/// <param name="InitialTail">The committed tail when the intent was first written (J0).</param>
/// <param name="Additions">Accumulated new member identifiers, without generation pins.</param>
/// <param name="Retirements">Requests to recheck against current connected declarations at commit.</param>
[GenerateSerializer]
[Alias("Cratis.Chronicle.Storage.Observation.ObserverTypeSetChange")]
public record ObserverTypeSetChange(
    [Id(0)] ObserverTypeSetRevision Revision,
    [Id(1)] DateTimeOffset Started,
    [Id(2)] EventSequenceNumber InitialTail,
    [Id(3)] IReadOnlyList<EventTypeId> Additions,
    [Id(4)] IReadOnlyList<ObserverEventTypeRetirement> Retirements);
