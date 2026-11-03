// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.MongoDB.Sinks;

/// <summary>
/// Represents the slot of a join that a changeset resolved into the key document.
/// </summary>
/// <param name="DefinitionId">The identity of the join definition.</param>
/// <param name="Stream">The identity of the event stream of the join event.</param>
/// <param name="SequenceNumber">The event sequence number of the join event.</param>
internal sealed record AppliedResolvedJoinSlot(string DefinitionId, string Stream, EventSequenceNumber SequenceNumber);
