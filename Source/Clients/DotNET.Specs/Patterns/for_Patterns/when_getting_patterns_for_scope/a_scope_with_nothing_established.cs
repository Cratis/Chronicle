// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Patterns;
using Cratis.Chronicle.Contracts.Queries;
using ProtoBuf.Grpc;
using Contract = Cratis.Chronicle.Contracts.Patterns;

namespace Cratis.Chronicle.Patterns.for_Patterns.when_getting_patterns_for_scope;

public class a_scope_with_nothing_established : given.a_patterns_client
{
    Contract.PatternsForScopeRequest _request;
    IEnumerable<BehaviorPattern> _result;

    void Establish() =>
        _patterns
            .PatternsForScope(Arg.Do<Contract.PatternsForScopeRequest>(request => _request = request), Arg.Any<CallContext>())
            .Returns(QueryResult<IEnumerable<Contract.BehaviorPatternDetailsResponse>>.Success(Guid.NewGuid(), []));

    async Task Because() => _result = await _client.GetPatternsForScope("user-42");

    [Fact] void should_ask_for_the_event_store() => _request.EventStore.ShouldEqual(EventStore);
    [Fact] void should_ask_for_the_namespace() => _request.Namespace.ShouldEqual(Namespace);
    [Fact] void should_ask_for_the_scope() => _request.GroupingKey.ShouldEqual("user-42");
    [Fact] void should_return_nothing() => _result.ShouldBeEmpty();
}
