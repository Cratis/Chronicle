// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.given;

namespace Cratis.Chronicle.Observation.Reducers.for_ReducerPipeline.when_replaying;

public class and_one_subject_was_erased : a_rebuild_with_erased_subjects
{
    JsonObject _liveStored;
    JsonObject _liveReleased;

    async Task Because()
    {
        await Reduce(Live);
        await Observe(Live);
        _liveStored = _stored;
        _liveReleased = _released;
        await Reduce(Erased);
        await Observe(Erased);
    }

    [Fact] void should_store_the_erased_leaf_as_empty() => _stored["profile"]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_erased_array_as_an_empty_marker() => _stored["privateTags"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_release_the_erased_array_as_empty() => _released["privateTags"]!.AsArray().ShouldBeEmpty();
    [Fact] void should_preserve_public_nested_state() => _released["profile"]!["status"]!.GetValue<string>().ShouldEqual("public");
    [Fact] void should_preserve_public_root_state() => _released["counter"]!.GetValue<int>().ShouldEqual(7);
    [Fact] void should_encrypt_the_live_control() => _liveStored["profile"]!["name"]!.GetValue<string>().ShouldNotEqual("live personal data");
    [Fact] void should_release_the_live_control() => _liveReleased["profile"]!["name"]!.GetValue<string>().ShouldEqual("live personal data");
    [Fact] void should_keep_both_documents() => _destination.Documents.Count.ShouldEqual(2);
    [Fact] async Task should_not_recreate_the_erased_key() => (await _keys.HasFor("store", "Default", Erased)).ShouldBeFalse();
    [Fact] async Task should_preserve_the_erasure_record() => (await _keys.GetErasureFor("store", "Default", Erased)).ShouldEqual(_erasure);
    [Fact] async Task should_preserve_the_live_key_and_revision() => (await _keys.TryGetLatestFor("store", "Default", Live)).ShouldEqual(_liveKey);
}
