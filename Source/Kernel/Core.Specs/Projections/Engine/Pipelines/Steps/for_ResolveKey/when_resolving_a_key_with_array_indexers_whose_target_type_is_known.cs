// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveKey;

public class when_resolving_a_key_with_array_indexers_whose_target_type_is_known : given.a_resolve_key_step
{
    ArrayIndexer[] _indexers;

    void Establish()
    {
        _indexers = [new ArrayIndexer(new PropertyPath("items"), new PropertyPath("id"), 42)];
        ResolverReturns(KeyResolverResult.Resolved(new Key("the-key", new ArrayIndexers(_indexers))));
    }

    async Task Because() => _result = await _resolveKey.Perform(_projection, _context);

    [Fact] void should_keep_the_key_value() => _result.Key.Value.ShouldEqual("the-key");
    [Fact] void should_keep_one_indexer() => _result.Key.ArrayIndexers.Count.ShouldEqual(1);
    [Fact] void should_keep_the_array_property() => _result.Key.ArrayIndexers.All.First().ArrayProperty.ShouldEqual(new PropertyPath("items"));
    [Fact] void should_keep_the_identifier_property() => _result.Key.ArrayIndexers.All.First().IdentifierProperty.ShouldEqual(new PropertyPath("id"));
    [Fact] void should_convert_the_identifier_to_the_schema_type() => _result.Key.ArrayIndexers.All.First().Identifier.ShouldEqual(42L);
}
