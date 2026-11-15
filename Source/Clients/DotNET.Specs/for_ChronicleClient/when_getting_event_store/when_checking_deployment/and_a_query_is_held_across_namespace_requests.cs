// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.EventStores;
using ProtoBuf.Grpc;

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment;

public class and_a_query_is_held_across_namespace_requests : given.a_serverless_client
{
    IEventStore _first;
    bool _unblocked;
    bool _readinessUnchanged;

    void Establish() => CreateClient();

    async Task Because()
    {
        var requests = await Task.WhenAll(_client.GetEventStore("store", "one"), _client.GetEventStore("store", "two"), _client.GetEventStore("store", "one")).WaitAsync(TimeSpan.FromSeconds(10));
        _first = requests[0];
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _unblocked = !_response.Task.IsCompleted;
        var before = _first.Registration;
        CompleteQuery(0, string.Empty);
        await _logger.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _readinessUnchanged = ReferenceEquals(before, _first.Registration);
    }

    [Fact] void should_not_block_callers() => _unblocked.ShouldBeTrue();
    [Fact] void should_query_once_for_the_store_across_namespaces() => _services.EventStores.Received(1).GetEventStoreDeployment(Arg.Is<GetEventStoreDeploymentRequest>(_ => _.EventStore == "store"), Arg.Any<CallContext>());
    [Fact] void should_warn_when_definitions_are_missing() => _logger.Level.ShouldEqual(Microsoft.Extensions.Logging.LogLevel.Warning);
    [Fact] void should_leave_readiness_unchanged() => _readinessUnchanged.ShouldBeTrue();
    [Fact] void should_not_register_definitions() => _services.EventStores.DidNotReceiveWithAnyArgs().RegisterDeployment(default!);
}
