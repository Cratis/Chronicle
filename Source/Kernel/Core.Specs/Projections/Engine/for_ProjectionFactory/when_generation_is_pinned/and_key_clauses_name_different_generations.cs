// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Projections.Engine.for_ProjectionFactory.when_generation_is_pinned;

public class and_key_clauses_name_different_generations : given.a_projection_with_generations
{
    ResolvedKey _firstKey;
    ResolvedKey _secondKey;

    async Task Because()
    {
        _projection = await _factory.Create("store", "namespace", _definition, _readModel, _schemas);
        _firstKey = (ResolvedKey)await _projection.GetKeyResolverFor(_first)(_sequence, NullSink.Instance, Event(_first, "original"));
        _secondKey = (ResolvedKey)await _projection.GetKeyResolverFor(_second)(_sequence, NullSink.Instance, Event(_second, "migrated"));
    }

    [Fact] void should_resolve_the_first_key_from_generation_one() => _firstKey.Key.Value.ShouldEqual("key-1");
    [Fact] void should_resolve_the_second_key_from_generation_two() => _secondKey.Key.Value.ShouldEqual("key-2");
}
