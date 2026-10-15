// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.MongoDB.Events.Constraints;

/// <summary>
/// Represents a retained unique value and its owner.
/// </summary>
/// <param name="Value">The retained hashed value.</param>
/// <param name="EventSourceId">The owning event source.</param>
/// <param name="SequenceNumber">The sequence number of the claim.</param>
public record UniqueConstraintValueIndex(UniqueConstraintValue Value, EventSourceId EventSourceId, EventSequenceNumber SequenceNumber);
