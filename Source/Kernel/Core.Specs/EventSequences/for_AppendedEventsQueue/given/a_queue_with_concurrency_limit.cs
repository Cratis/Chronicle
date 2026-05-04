// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Cratis.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cratis.Chronicle.EventSequences.for_AppendedEventsQueue.given;

/// <summary>
/// A given context that constructs an <see cref="AppendedEventsQueue"/> with a specific
/// <see cref="Configuration.Events.MaxConcurrentObserverDispatches"/> limit.
/// </summary>
public abstract class a_queue_with_concurrency_limit : all_dependencies
{
    protected AppendedEventsQueue _queue;
    protected int _maxConcurrentDispatches;

    void Establish()
    {
        _maxConcurrentDispatches = MaxConcurrentDispatches;
        var options = Options.Create(new ChronicleOptions
        {
            Events = new Configuration.Events
            {
                MaxConcurrentObserverDispatches = _maxConcurrentDispatches,
                QueueBoundedCapacity = 0
            }
        });

        _queue = new AppendedEventsQueue(
            _taskFactory,
            _grainFactory,
            Substitute.For<IMeter<AppendedEventsQueue>>(),
            options,
            Substitute.For<ILogger<AppendedEventsQueue>>());
    }

    /// <summary>
    /// Gets the maximum concurrent observer dispatches for the queue under test.
    /// </summary>
    protected abstract int MaxConcurrentDispatches { get; }
}
