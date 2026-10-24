// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.Reducers.for_ReducerDispatcher.when_dispatching;

/// <summary>A converted reducer failure retains its exception type on the span.</summary>
public class and_a_failure_is_traced : given.a_reducer_dispatcher
{
    ActivityListener _listener;
    ActivityTraceId _trace;
    Activity _handle;

    void Establish()
    {
        _listener = new()
        {
            ShouldListenTo = _ => true,
            Sample = (ref _) => ActivitySamplingResult.AllData,
            ActivityStopped = activity =>
            {
                if (activity.TraceId == _trace && activity.OperationName == "client.reducer.handle") _handle = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
        _handler.OnNext(Arg.Any<IEnumerable<AppendedEvent>>(), Arg.Any<object?>(), Arg.Any<IServiceProvider>()).Returns(new ReduceResult(null, EventSequenceNumber.Unavailable, ["private message"], string.Empty) { ErrorType = typeof(InvalidOperationException).FullName });
    }

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _dispatcher.Dispatch(_handler, OperationWith(1), "ns", Publish);
    }

    [Fact] void should_mark_the_span_as_failed() => _handle.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_exception_type() => _handle.GetTagItem("error.type").ShouldEqual(typeof(InvalidOperationException).FullName);
    [Fact] void should_not_record_the_message() => string.IsNullOrEmpty(_handle.StatusDescription).ShouldBeTrue();

    void Destroy() => _listener.Dispose();
}
