// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;

namespace Cratis.Chronicle.Events.for_EventGenerationRelease.when_releasing;

public class and_the_pinned_generation_is_stored : given.a_release_boundary
{
    async Task Because() => _result = await _release.Release(_event.Context.EventStore, [_pin], _schemas, [_event]);

    [Fact] void should_deliver_the_pinned_shape_released() => ((IDictionary<string, object?>)_result[0].Content)["fullName"].ShouldEqual("Ada Lovelace");
    [Fact] void should_deliver_the_pinned_context() => _result[0].Context.EventType.ShouldEqual(_pin);
    [Fact] void should_deliver_the_pinned_hash() => _result[0].Context.Hash.Value.ShouldEqual("second-hash");
    [Fact] void should_keep_the_appended_generation() => _result[0].Context.AppendedGeneration!.Value.ShouldEqual(1U);
    [Fact] void should_keep_generational_content() => _result[0].GenerationalContent.ShouldEqual(_event.GenerationalContent);
    [Fact] async Task should_release_with_the_pinned_schema() => await _metadata.Received(1).Release(Arg.Any<EventStoreName>(), Arg.Any<EventStoreNamespaceName>(), _schema, "person", Arg.Any<System.Text.Json.Nodes.JsonObject>());
}
