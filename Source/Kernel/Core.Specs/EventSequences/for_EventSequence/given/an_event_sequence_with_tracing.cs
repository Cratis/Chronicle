// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.given;

public class an_event_sequence_with_tracing : an_event_sequence
{
    protected ActivityTraceId _trace;
    protected Activity _appendActivity;
    ActivityListener _listener;

    void Establish()
    {
        _listener = new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == _trace && activity.OperationName == "cratis.chronicle.event_sequence.append") _appendActivity = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
        _eventSourceId = "customer-42";
    }

    void Destroy() => _listener.Dispose();
}
