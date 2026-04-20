// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle;

/// <summary>
/// Represents the event that gets appended when an event store is added.
/// </summary>
/// <param name="EventStore">The <see cref="EventStoreName"/> that was added.</param>
[EventType]
public record EventStoreAdded(EventStoreName EventStore);
