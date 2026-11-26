// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveKey;

public class when_resolving_a_key_with_an_array_indexer_whose_property_is_not_in_the_schema : given.a_resolve_key_step
{
    ArrayIndexers _indexers;

    void Establish()
    {
        _indexers = new ArrayIndexers([new ArrayIndexer(new PropertyPath("unknown"), new PropertyPath("id"), 42)]);
        ResolverReturns(KeyResolverResult.Resolved(new Key("the-key", _indexers)));
    }

    async Task Because() => _result = await _resolveKey.Perform(_projection, _context);

    [Fact] void should_leave_the_indexer_unchanged() => _result.Key.ArrayIndexers.ShouldEqual(_indexers);
    [Fact] void should_keep_the_identifier_as_is() => _result.Key.ArrayIndexers.All.First().Identifier.ShouldEqual(42);
    [Fact] void should_keep_the_key_equal_to_the_resolved_key() => _result.Key.ShouldEqual(new Key("the-key", _indexers));
}
