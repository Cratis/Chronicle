// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace Cratis.Chronicle.Diagnostics.OpenTelemetry.Tracing.for_DetachedExecution;

public class when_running_inside_an_activity : Specification
{
    Activity _inside;
    Activity _after;
    Activity _ambient;
    Activity _inside_started_work;

    async Task Because()
    {
        using var ambient = new Activity("caller").Start();
        _ambient = ambient;
        await DetachedExecution.Run(() =>
        {
            _inside = Activity.Current;
            return Task.CompletedTask;
        });
        await DetachedExecution.Start(() => Task.Run(() => _inside_started_work = Activity.Current));
        _after = Activity.Current;
    }

    [Fact] void should_not_inherit_the_activity() => _inside.ShouldBeNull();
    [Fact] void should_not_inherit_the_activity_when_starting_work() => _inside_started_work.ShouldBeNull();
    [Fact] void should_restore_the_callers_activity() => _after.ShouldEqual(_ambient);
}
