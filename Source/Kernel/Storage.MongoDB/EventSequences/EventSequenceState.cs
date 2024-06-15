// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Storage.MongoDB;

/// <summary>
/// Represents the persisted state of an event sequence for MongoDB.
/// </summary>
/// <param name="SequenceNumber">The <see cref="EventSequenceNumber"/> representing the tail of the sequence.</param>
/// <param name="TailSequenceNumberPerEventType">Tail <see cref="EventSequenceNumber"/> per event type id.</param>
public record EventSequenceState(
    EventSequenceNumber SequenceNumber,
    IDictionary<string, EventSequenceNumber> TailSequenceNumberPerEventType);
