// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Traces;

namespace Cratis.Chronicle.Observation.for_Observer.given;

public class a_traced_observer : an_observer_with_subscription_for_specific_event_type
{
    protected readonly List<Activity> _activities = [];
    readonly ActivitySource _source = new($"Chronicle.Observer.Specs.{Guid.NewGuid():N}");
    ActivityListener _listener;

    protected override IActivitySource<Observer> CreateActivitySource() => new ActivitySource<Observer>(_source);

    void Establish()
    {
        _listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == _source.Name,
            Sample = (ref _) => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStopped = activity => _activities.Add(activity)
        };
        ActivitySource.AddActivityListener(_listener);
    }

    void Destroy()
    {
        _listener.Dispose();
        _source.Dispose();
    }
}
