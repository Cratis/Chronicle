// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveKey;

public class when_resolving_a_key_without_array_indexers : given.a_resolve_key_step
{
    Key _resolved;

    void Establish()
    {
        _resolved = new Key("the-key", ArrayIndexers.NoIndexers);
        ResolverReturns(KeyResolverResult.Resolved(_resolved));
    }

    async Task Because() => _result = await _resolveKey.Perform(_projection, _context);

    [Fact] void should_set_the_resolved_key() => _result.Key.ShouldEqual(_resolved);
    [Fact] void should_keep_the_key_value() => _result.Key.Value.ShouldEqual("the-key");
    [Fact] void should_have_no_array_indexers() => _result.Key.ArrayIndexers.IsEmpty.ShouldBeTrue();
    [Fact] void should_equal_the_no_indexers_instance() => _result.Key.ArrayIndexers.ShouldEqual(ArrayIndexers.NoIndexers);
    [Fact] void should_use_the_key_value_as_join_key() => _result.JoinKey.ShouldEqual("the-key");
}
