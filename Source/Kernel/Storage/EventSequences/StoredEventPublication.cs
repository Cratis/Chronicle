// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences;

/// <summary>
/// Represents a durable event's sequence number and immutable fingerprint, without comparing intent.
/// </summary>
/// <param name="SequenceNumber">The sequence number of the durable event.</param>
/// <param name="Fingerprint">The fingerprint stored by the original append.</param>
public record StoredEventPublication(EventSequenceNumber SequenceNumber, string Fingerprint);
