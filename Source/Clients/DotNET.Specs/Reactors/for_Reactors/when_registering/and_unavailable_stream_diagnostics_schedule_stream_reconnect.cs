// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_unavailable_stream_diagnostics_schedule_stream_reconnect : given.an_observed_reactor_stream
{
    readonly given.RecordingReactorLogger _diagnostics = new();

    protected override ILogger<Reactors> CreateLogger() => _diagnostics;

    void Establish() => _reactors.Register<MyReactor>().GetAwaiter().GetResult();

    async Task Because()
    {
        await _reactors.Register();
        _observed.OnError(new RpcException(new Status(StatusCode.Unavailable, "private-status-detail")));
    }

    [Fact] void should_record_the_status_code() => _diagnostics.States.ShouldContain(state => state.TryGetValue("RpcStatus", out var status) && Equals(status, "Unavailable"));
    [Fact] void should_record_the_existing_stream_reconnect_decision() => _diagnostics.States.ShouldContain(state => state.TryGetValue("Outcome", out var outcome) && Equals(outcome, "reconnect-scheduled"));
    [Fact] void should_not_request_connection_reconnect() => _eventStore.Connection.DidNotReceive().RequestReconnect();
    [Fact] void should_not_log_status_details() => _diagnostics.States.SelectMany(state => state.Values).ShouldNotContain(value => value is string text && text.Contains("private-status-detail"));

    class MyReactor : IReactor;
}
