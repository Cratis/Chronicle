// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Properties;
using Cratis.Chronicle.Schemas;
using Cratis.Monads;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_identifying_read_model_key_for_keyed_child_update;

public class and_the_parent_type_is_recursive : given.a_flat_child_projection_with_keyed_events
{
    KeyResolverResult _result;

    void Establish()
    {
        var schema = JsonSchema.FromJson("""{"title":"Node","type":"object","properties":{"id":{"type":"string"}}}""");
        RootProjection.TargetReadModelSchema.Returns(schema);
        ChildProjection.TargetReadModelSchema.Returns(JsonSchema.FromJson(schema.ToJson()));
        RootProjection.ChildProjections.Returns([ChildProjection]);
        Sink.TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ChildKey)
            .Returns(new Option<Key>(new Key(BoardKey, ArrayIndexers.NoIndexers)));
        Storage.WithParentHistory(BoardKey, RootCreatedEvent);
    }

    async Task Because() => _result = await CreateResolverUnderTest()(Storage, Sink, ChildMovedEvent);

    [Fact] void should_resolve_the_root_key() => (_result as ResolvedKey).Key.Value.ShouldEqual(BoardKey);
    [Fact] void should_resolve_the_child_indexer() => (_result as ResolvedKey).Key.ArrayIndexers.GetFor("children").Identifier.ShouldEqual(ChildKey);
    [Fact] void should_not_route_using_a_sink_hit_that_only_identifies_the_document() =>
        Sink.DidNotReceive().TryFindRootKeyByChildValue(Arg.Any<PropertyPath>(), ChildKey);
}
