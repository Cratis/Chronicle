// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Linq;

namespace Cratis.Chronicle.EventSequences;

/// <summary>Shares append subscriptions between a cached event sequence and its namespace-bound writers.</summary>
internal sealed class EventSequenceAppendNotifications
{
    IObservable<IEnumerable<AppendedEventWithResult>>? _operations;
    event Action<IEnumerable<AppendedEventWithResult>>? _appendedEventsRaised;

    /// <summary>Gets the shared observable for append results.</summary>
    public IObservable<IEnumerable<AppendedEventWithResult>> Operations =>
        _operations ??= Observable.FromEvent<IEnumerable<AppendedEventWithResult>>(
            handler => _appendedEventsRaised += handler,
            handler => _appendedEventsRaised -= handler);

    /// <summary>Gets a snapshot of the current append subscribers.</summary>
    public Action<IEnumerable<AppendedEventWithResult>>? Handler => _appendedEventsRaised;
}
