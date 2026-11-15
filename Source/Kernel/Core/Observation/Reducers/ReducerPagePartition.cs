// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Keys;

namespace Cratis.Chronicle.Observation.Reducers;

/// <summary>
/// Represents one partition of a page to reduce: its events and the state its read model holds.
/// </summary>
/// <param name="Key">The <see cref="Key"/> of the partition.</param>
/// <param name="Events">The events of the partition, in sequence order.</param>
/// <param name="InitialState">The state to reduce from, or null when the partition has no read model yet.</param>
public record ReducerPagePartition(Key Key, IEnumerable<AppendedEvent> Events, ExpandoObject? InitialState);
