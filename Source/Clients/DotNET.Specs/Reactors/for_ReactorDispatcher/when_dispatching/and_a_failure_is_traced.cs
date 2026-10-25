// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Reactors.for_ReactorDispatcher.when_dispatching;

/// <summary>A caught handler exception marks the client span.</summary>
public class and_a_failure_is_traced : given.a_reactor_dispatcher
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
                if (activity.TraceId == _trace && activity.OperationName == "client.reactor.handle") _handle = activity;
            }
        };
        ActivitySource.AddActivityListener(_listener);
    }

    async Task Because()
    {
        using var root = new Activity("caller").Start();
        _trace = root.TraceId;
        await _dispatcher.Dispatch(_handler, (_, _) => throw new InvalidOperationException("private message"), EventsWith(1), "ns", Publish, CancellationToken.None);
    }

    [Fact] void should_mark_the_span_as_failed() => _handle.Status.ShouldEqual(ActivityStatusCode.Error);
    [Fact] void should_record_the_exception_type() => _handle.GetTagItem("error.type").ShouldEqual(typeof(InvalidOperationException).FullName);
    [Fact] void should_not_record_the_message() => string.IsNullOrEmpty(_handle.StatusDescription).ShouldBeTrue();

    void Destroy() => _listener.Dispose();
}
