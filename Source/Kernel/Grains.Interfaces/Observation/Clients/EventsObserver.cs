// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Grains.Observation.Clients;

/// <summary>
/// Delegate that gets called when events are to be observed.
/// </summary>
/// <param name="events">Collection of <see cref="AppendedEvent"/> to observe.</param>
/// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
public delegate void EventsObserver(IEnumerable<AppendedEvent> events, TaskCompletionSource<ObserverSubscriberResult> taskCompletionSource);
