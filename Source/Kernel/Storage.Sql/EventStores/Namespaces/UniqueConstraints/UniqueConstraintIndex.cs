// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.UniqueConstraints;

/// <summary>
/// Represents the index object for a unique constraint.
/// </summary>
/// <param name="EventSourceId">The <see cref="EventSourceId"/> for the index.</param>
/// <param name="Value">The <see cref="UniqueConstraintValue"/> for the index.</param>
/// <param name="SequenceNumber"><see cref="EventSequenceNumber"/> where the value can be seen.</param>
public record UniqueConstraintIndex(EventSourceId EventSourceId, UniqueConstraintValue Value, EventSequenceNumber SequenceNumber);
