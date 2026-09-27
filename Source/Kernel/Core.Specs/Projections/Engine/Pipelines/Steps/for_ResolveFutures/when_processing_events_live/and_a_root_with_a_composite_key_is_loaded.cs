// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Projections.Engine.Pipelines.Steps.for_ResolveFutures.when_processing_events_live;

public class and_a_root_with_a_composite_key_is_loaded : given.a_first_level_child_future
{
    bool _childWithIndependentKey;
    bool _childWithSerializedKey;
    object _saveKeyForSerializedRoot;

    void Establish()
    {
        dynamic key = new ExpandoObject();
        key.tenant = "a";
        key.order = 42;
        SetFutureKey((ExpandoObject)key);
    }

    async Task Because()
    {
        dynamic key = new ExpandoObject();
        key.tenant = "a";
        key.order = 42;
        await ProcessRoot(RootWith(), (ExpandoObject)key);
        _childWithIndependentKey = HasChild;

        _resolved = false;
        _tracker.HasPending = true;
        await ProcessRoot(RootWith(), "{\"tenant\":\"a\",\"order\":42}");
        _childWithSerializedKey = HasChild;
        _saveKeyForSerializedRoot = _result!.PendingFutureSaves.Single().Key.Value;
    }

    [Fact] void should_match_equivalent_composite_values() => _childWithIndependentKey.ShouldBeTrue();
    [Fact] void should_match_a_serialized_composite_key() => _childWithSerializedKey.ShouldBeTrue();
    [Fact] void should_save_to_the_same_serialized_root_key() => _saveKeyForSerializedRoot.ShouldEqual("{\"tenant\":\"a\",\"order\":42}");
}
