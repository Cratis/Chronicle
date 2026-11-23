// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.for_KeyResolvers.when_resolving_a_grandchild_from_the_sink.given;

public class two_roots_with_children : when_identifying_read_model_key_for_nested_event_within_grandchild_projection.given.a_three_level_hierarchy
{
    protected const string FirstRoot = "root-a";
    protected const string SecondRoot = "root-b";
    protected Key _result;
    PreviewSink _rootSink;

    void Establish()
    {
        _rootSink = new PreviewSink("id");
        Storage.WithParentHistory(FeatureKey);
    }

    protected void AddRoot(string root, string child)
    {
        var document = new
        {
            id = root,
            features = new[] { new { featureId = child } }
        }.AsExpandoObject();
        _rootSink.Set(root, document, 1);
    }

    protected async Task<Key> Resolve(string root)
    {
        var @event = CreateEvent(3, SliceAddedEventType, root, new { featureId = FeatureKey, sliceId = SliceKey }.AsExpandoObject());
        var resolver = KeyResolvers.FromParentHierarchy(
            SliceProjection,
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("sliceId")),
            KeyResolvers.FromEventValueProvider(EventValueProviders.EventContent("featureId")),
            "sliceId");
        var result = await resolver(Storage, _rootSink, @event);
        return ((ResolvedKey)result).Key;
    }
}
