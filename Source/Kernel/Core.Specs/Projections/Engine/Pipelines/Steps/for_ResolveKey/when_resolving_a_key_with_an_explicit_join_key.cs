// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveKey;

public class when_resolving_a_key_with_an_explicit_join_key : given.a_resolve_key_step
{
    void Establish() => ResolverReturns(KeyResolverResult.Resolved(new Key("the-key", ArrayIndexers.NoIndexers), "the-join-key"));

    async Task Because() => _result = await _resolveKey.Perform(_projection, _context);

    [Fact] void should_use_the_explicit_join_key() => _result.JoinKey.ShouldEqual("the-join-key");
    [Fact] void should_set_the_resolved_key() => _result.Key.Value.ShouldEqual("the-key");
}
