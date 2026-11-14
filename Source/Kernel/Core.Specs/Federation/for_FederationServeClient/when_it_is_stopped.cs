// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_it_is_stopped : given.a_serve_client
{
    int _callsAtStop;
    int _callsLater;

    async Task Because()
    {
        _serve.Script.Add(new ServePullRequest { Status = FederatedEventBatchStatus.Denied });
        Start();
        await Eventually(() => _serve.Calls >= 1);
        _client.Stop();
        await Task.Delay(50);
        _callsAtStop = _serve.Calls;
        await Task.Delay(150);
        _callsLater = _serve.Calls;
    }

    [Fact] void should_stop_opening_calls() => _callsLater.ShouldEqual(_callsAtStop);
    [Fact] void should_not_be_serving() => _client.IsServing.ShouldBeFalse();
}
