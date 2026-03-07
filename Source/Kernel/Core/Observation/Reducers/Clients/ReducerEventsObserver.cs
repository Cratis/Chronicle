// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.Reducers.Clients;

/// <summary>
/// Delegate that gets called when events are to be observed.
/// </summary>
/// <param name="context"><see cref="ReduceOperation"/> to use for the observation.</param>
/// <param name="taskCompletionSource"><see cref="TaskCompletionSource{T}"/> to return <see cref="ObserverSubscriberResult"/> to.</param>
public delegate void ReducerEventsObserver(ReduceOperation context, TaskCompletionSource<ReducerSubscriberResult> taskCompletionSource);
