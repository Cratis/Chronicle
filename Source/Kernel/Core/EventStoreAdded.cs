// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the event that gets appended when an event store is added.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> that was added.</param>
[EventType("8a8d7c3e-4f6b-4e5a-9c7d-2b1f3e4a5d6c")]
public record EventStoreAdded(EventStoreName EventStore);
