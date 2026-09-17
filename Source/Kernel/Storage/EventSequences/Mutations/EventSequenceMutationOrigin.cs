// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents the event that originated an event sequence mutation.
/// </summary>
/// <param name="Sequence">The validated event sequence containing the originating event.</param>
/// <param name="SequenceNumber">The sequence number of the originating event.</param>
public sealed record EventSequenceMutationOrigin(EventSequenceMutationIdentity Sequence, EventSequenceNumber SequenceNumber);
