// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Contracts.Federation;

namespace Cratis.Chronicle.Federation.for_FederationServeClient;

public class when_only_some_event_types_are_served : given.a_serve_client
{
    async Task Because()
    {
        _serveEventTypes = ["type-a"];
        _serve.Script.Add(new ServePullRequest { RequestId = 1, FromSequenceNumber = 0, Limit = 10, EventTypes = ["type-a", "type-b"] });
        Start();
        await Eventually(() => _serve.Received.Count == 1);
    }

    [Fact] void should_grant_only_the_listed_types() => _reads.Single().Grants.Single().AllPublic.ShouldBeFalse();
    [Fact] void should_grant_exactly_the_listed_types() => _reads.Single().Grants.Single().EventTypes.Select(_ => _.Value).ToArray().ShouldEqual(["type-a"]);
}
