// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_DetachedExecution;

public class and_flow_is_already_suppressed : Specification
{
    Activity _inside;
    Activity _inside_started_work;
    bool _stillSuppressed;

    async Task Because()
    {
        Task running;
        Task starting;
        using (ExecutionContext.SuppressFlow())
        {
            running = DetachedExecution.Run(() =>
            {
                _inside = Activity.Current;
                return Task.CompletedTask;
            });
            starting = DetachedExecution.Start(() => Task.Run(() => _inside_started_work = Activity.Current));
            _stillSuppressed = ExecutionContext.IsFlowSuppressed();
        }

        await Task.WhenAll(running, starting);
    }

    [Fact] void should_not_inherit_the_activity() => _inside.ShouldBeNull();
    [Fact] void should_not_inherit_the_activity_when_starting_work() => _inside_started_work.ShouldBeNull();
    [Fact] void should_preserve_the_existing_suppression() => _stillSuppressed.ShouldBeTrue();
}
