// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents a publication identity committed atomically with its event.
/// </summary>
/// <param name="SequenceNumber">The durable event's sequence number, not the slot proposed by a retry.</param>
public record EventPublicationReceipt(EventSequenceNumber SequenceNumber);
