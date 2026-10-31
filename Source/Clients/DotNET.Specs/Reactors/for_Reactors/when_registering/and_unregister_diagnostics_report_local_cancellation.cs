// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_unregister_diagnostics_report_local_cancellation : given.an_observed_reactor_stream
{
    readonly given.RecordingReactorLogger _diagnostics = new();

    protected override ILogger<Reactors> CreateLogger() => _diagnostics;

    void Establish() => _reactors.Register<MyReactor>().GetAwaiter().GetResult();

    async Task Because()
    {
        await _reactors.Register();
        _reactors.Unregister(_reactors.GetHandlerFor<MyReactor>().Id);
    }

    [Fact] void should_record_the_explicit_unregister() => _diagnostics.States.ShouldContain(state => state.TryGetValue("Outcome", out var outcome) && Equals(outcome, "unregister"));
    [Fact] void should_record_local_handler_cancellation() => _diagnostics.States.ShouldContain(state => state.TryGetValue("Outcome", out var outcome) && Equals(outcome, "handler-cancelled") && Equals(state["LocalCancellation"], true));

    class MyReactor : IReactor;
}
