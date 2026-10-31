// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_completed_stream_diagnostics_follow_reregistration : given.an_observed_reactor_stream
{
    readonly given.RecordingReactorLogger _diagnostics = new();

    protected override ILogger<Reactors> CreateLogger() => _diagnostics;

    void Establish() => _reactors.Register<MyReactor>().GetAwaiter().GetResult();

    async Task Because()
    {
        await _reactors.Register();
        _observed.OnCompleted();
        await _diagnostics.ReRegistered.Task.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact] void should_record_graceful_completion() => _diagnostics.States.ShouldContain(state => state.ContainsKey("Terminal") && Equals(state["Terminal"], "Completed"));
    [Fact] void should_record_the_existing_reconnect_decision() => Outcomes().ShouldContain("reconnect-scheduled");
    [Fact] void should_record_the_existing_reconnect_execution() => Outcomes().ShouldContain("reconnect-executing");
    [Fact] void should_register_a_second_stream() => Outcomes().Count(outcome => Equals(outcome, "stream-started")).ShouldEqual(2);

    IEnumerable<object?> Outcomes() => _diagnostics.States.Where(state => state.ContainsKey("Outcome")).Select(state => state["Outcome"]);

    class MyReactor : IReactor;
}
