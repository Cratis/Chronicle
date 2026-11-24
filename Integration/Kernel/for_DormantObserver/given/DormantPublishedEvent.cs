// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Kernel.Integration.for_DormantObserver.given;

/// <summary>
/// An event a source event store publishes to its outbox, for an event store subscription to forward.
/// </summary>
/// <param name="Name">The name of the event.</param>
[EventType("dormant-published-event")]
[Public]
public record DormantPublishedEvent(string Name);
