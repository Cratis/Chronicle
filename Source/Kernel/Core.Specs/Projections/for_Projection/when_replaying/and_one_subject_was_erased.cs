// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels.for_ReadModelsCompliance.given;

namespace Cratis.Chronicle.Projections.for_Projection.when_replaying;

public class and_one_subject_was_erased : a_rebuild_with_erased_subjects
{
    Task Establish() => CreateProjection();

    Task Because() => ReplayProjection();

    [Fact] void should_store_the_erased_leaf_as_an_empty_marker() => _stored["profile"]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_store_the_erased_whole_boundary_without_content() => _stored["privateTags"]!.AsArray().ShouldBeEmpty();
    [Fact] void should_release_the_erased_array_as_an_empty_array() => _released["privateTags"]!.AsArray().ShouldBeEmpty();
    [Fact] void should_release_the_erased_leaf_as_empty() => _released["profile"]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_the_public_nested_sibling() => _released["profile"]!["status"]!.GetValue<string>().ShouldEqual("public");
    [Fact] void should_preserve_the_public_counter() => _released["counter"]!.GetValue<int>().ShouldEqual(7);
    [Fact] void should_encrypt_the_live_value_at_rest() => _stored["liveName"]!.GetValue<string>().ShouldNotEqual("live personal data");
    [Fact] void should_release_the_live_value() => _released["liveName"]!.GetValue<string>().ShouldEqual("live personal data");
    [Fact] void should_record_the_explicit_erased_subject_not_the_event_source() => _stored["__subject"]!.GetValue<string>().ShouldEqual(Erased);
    [Fact] void should_record_the_contributing_live_owner() => _stored["__subjects"]!["liveName"]!.GetValue<string>().ShouldEqual(Live);
    [Fact] void should_not_copy_the_orphan() => _destination.Documents.ContainsKey("orphan").ShouldBeFalse();
    [Fact] void should_not_copy_the_obsolete_property() => _stored.ContainsKey("obsolete").ShouldBeFalse();
    [Fact] void should_leave_the_old_container_separate() => _old.Documents.Count.ShouldEqual(2);
    [Fact] async Task should_not_recreate_the_erased_key() => (await _keys.HasFor("store", "Default", Erased)).ShouldBeFalse();
    [Fact] async Task should_preserve_the_erasure_record() => (await _keys.GetErasureFor("store", "Default", Erased)).ShouldEqual(_erasure);
    [Fact] async Task should_preserve_the_live_key_and_revision() => (await _keys.TryGetLatestFor("store", "Default", Live)).ShouldEqual(_liveKey);
}
