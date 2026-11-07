// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Grpc.Core;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.for_ChronicleClient.when_checking_deployment;

public class and_the_rpc_is_unimplemented : given.a_serverless_client
{
    IEventStore _store;
    bool _unchanged;

    void Establish() => CreateClient();

    async Task Because()
    {
        _store = await _client.GetEventStore("store", "one");
        await _entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        var before = _store.Registration;
        _response.SetException(new RpcException(new Status(StatusCode.Unimplemented, "old kernel")));
        await _logger.Finished.Task.WaitAsync(TimeSpan.FromSeconds(10));
        _unchanged = ReferenceEquals(before, _store.Registration);
        await _client.GetEventStore("store", "two");
    }

    [Fact] void should_observe_the_failure() => _logger.Failure.ShouldBeOfExactType<RpcException>();
    [Fact] void should_warn_without_breaking_callers() => _logger.Level.ShouldEqual(LogLevel.Warning);
    [Fact] void should_not_change_readiness() => _unchanged.ShouldBeTrue();
    [Fact] void should_not_retry_on_another_namespace() => _services.EventStores.Received(1).GetEventStoreDeployment(Arg.Any<Contracts.EventStores.GetEventStoreDeploymentRequest>(), Arg.Any<ProtoBuf.Grpc.CallContext>());
}
