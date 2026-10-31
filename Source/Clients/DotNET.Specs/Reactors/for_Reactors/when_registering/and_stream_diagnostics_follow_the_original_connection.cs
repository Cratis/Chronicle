// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Reactors.for_Reactors.when_registering;

public class and_stream_diagnostics_follow_the_original_connection : given.an_observed_reactor_stream
{
    readonly given.RecordingReactorLogger _diagnostics = new();

    protected override ILogger<Reactors> CreateLogger() => _diagnostics;

    void Establish() => _reactors.Register<MyReactor>().GetAwaiter().GetResult();

    async Task Because()
    {
        await _reactors.Register();
        _connectionLifecycle.ConnectionId.Returns((ConnectionId)"replacement-connection");
        _observed.OnError(new RpcException(new Status(StatusCode.Cancelled, "private-status-detail")));
    }

    [Fact] void should_log_the_original_connection() => Terminals().ShouldContain(state => Equals(state["ConnectionId"], "test-connection-id"));
    [Fact] void should_not_log_the_replacement_connection() => Terminals().ShouldNotContain(state => Equals(state["ConnectionId"], "replacement-connection"));
    [Fact] void should_log_only_the_rpc_status_code() => Terminals().ShouldContain(state => Equals(state["RpcStatus"], "Cancelled"));
    [Fact] void should_not_log_status_details() => _diagnostics.States.SelectMany(state => state.Values).ShouldNotContain(value => value is string text && text.Contains("private-status-detail"));
    [Fact] void should_distinguish_remote_cancellation_from_local_cancellation() => Terminals().ShouldContain(state => Equals(state["LocalCancellation"], false));

    IEnumerable<IReadOnlyDictionary<string, object?>> Terminals() => _diagnostics.States.Where(state => state.ContainsKey("Terminal"));

    class MyReactor : IReactor;
}
