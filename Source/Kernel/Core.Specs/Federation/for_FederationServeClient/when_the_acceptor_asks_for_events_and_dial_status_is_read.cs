// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_the_acceptor_asks_for_events_and_dial_status_is_read : given.a_serve_client
{
    FederationDialStatus _before;

    async Task Because()
    {
        _before = _dialStatus.GetDial();
        _serve.Script.Add(new ServePullRequest { RequestId = 1, FromSequenceNumber = 0, Limit = 10, EventTypes = ["type-a"] });
        Start();
        await _serve.WhenReceived(1);
    }

    [Fact] void should_not_be_connected_before_anything_is_answered() => _before.ShouldEqual(FederationDialStatus.None);
    [Fact] void should_be_connected_once_a_request_is_answered() => _dialStatus.GetDial().Connected.ShouldBeTrue();
    [Fact] void should_record_the_time_of_the_exchange() => _dialStatus.GetDial().LastExchange.ShouldNotBeNull();
}
