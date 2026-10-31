// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_cancellation_diagnostics_follow_the_registered_session_after_rotation : given.an_observed_reactor_stream
{
    readonly given.RecordingReactorLogger _diagnostics = new();
    CancellationToken _originalToken;
    CancellationToken _replacementToken;
    IReadOnlyDictionary<string, object?>[] _cancellations = [];

    protected override ILogger<Reactors> CreateLogger() => _diagnostics;

    void Establish() => _reactors.Register<MyReactor>().GetAwaiter().GetResult();

    async Task Because()
    {
        await _reactors.Register();
        _originalToken = _reactors.GetHandlerFor<MyReactor>().CancellationToken;
        _connectionLifecycle.OnDisconnected += Raise.Event<Disconnected>();
        var replacement = _reactors.GetHandlerFor<MyReactor>();
        _replacementToken = replacement.CancellationToken;
        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"registered-replacement");
        await _reactors.Register();
        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"later-unregistered-session");
        _reactors.Unregister(replacement.Id);
        _cancellations = _diagnostics.States.Where(state => state.TryGetValue("Outcome", out var outcome) && Equals(outcome, "handler-cancelled")).ToArray();
    }

    [Fact] void should_preserve_old_handler_cancellation() => _originalToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_preserve_replacement_handler_cancellation() => _replacementToken.IsCancellationRequested.ShouldBeTrue();
    [Fact] void should_record_both_cancellations() => _cancellations.Length.ShouldEqual(2);
    [Fact] void should_keep_the_original_session_for_the_old_handler() => _cancellations[0]["ConnectionId"].ShouldEqual("test-connection-id");
    [Fact] void should_use_the_registered_session_not_the_replacement_creation_session() => _cancellations[1]["ConnectionId"].ShouldEqual("registered-replacement");
    [Fact] void should_use_the_registered_session_for_the_explicit_disconnect() => _diagnostics.States.ShouldContain(state => state.TryGetValue("Outcome", out var outcome) && Equals(outcome, "unregister") && Equals(state["ConnectionId"], "registered-replacement"));
    [Fact] void should_not_correlate_any_cancellation_to_a_later_session() => _cancellations.ShouldNotContain(state => Equals(state["ConnectionId"], "later-unregistered-session"));

    class MyReactor : IReactor;
}
