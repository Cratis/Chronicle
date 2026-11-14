// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_the_acceptor_asks_for_events : given.a_serve_client
{
    async Task Because()
    {
        _serve.Script.Add(new ServePullRequest { RequestId = 7, FromSequenceNumber = 5, Limit = 50, EventTypes = ["type-a"] });
        _serve.Script.Add(new ServePullRequest { RequestId = 8, FromSequenceNumber = 9, Limit = 50, EventTypes = ["type-a", "type-b"] });
        Start();
        await Eventually(() => _serve.Received.Count == 2);
    }

    [Fact] void should_answer_each_request_with_its_own_id() => _serve.Received.Select(_ => _.RequestId).ToArray().ShouldEqual([7UL, 8UL]);
    [Fact] void should_read_the_outbox_of_the_configured_store_and_namespace() => _reads.TrueForAll(_ => _.Store == "local" && _.Namespace == "ns").ShouldBeTrue();
    [Fact] void should_read_from_where_the_acceptor_asked() => _reads.Select(_ => _.From).ToArray().ShouldEqual([5UL, 9UL]);
    [Fact] void should_not_read_more_than_the_acceptor_asked_for() => _reads.TrueForAll(_ => _.Limit == 50).ShouldBeTrue();
    [Fact] void should_only_read_what_the_acceptor_wants() => _reads[1].Types.ShouldEqual(["type-a", "type-b"]);
    [Fact] void should_serve_every_public_event_type_when_none_is_listed() => _reads[0].Grants.Single().AllPublic.ShouldBeTrue();
    [Fact] void should_be_serving() => _client.IsServing.ShouldBeTrue();
}
