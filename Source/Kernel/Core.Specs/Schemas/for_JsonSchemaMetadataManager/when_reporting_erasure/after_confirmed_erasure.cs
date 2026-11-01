// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;

namespace Cratis.Chronicle.Schemas.for_JsonSchemaMetadataManager.when_reporting_erasure;

public class after_confirmed_erasure : given.content_for_erasure_reporting
{
    JsonObject _legacy;

    async Task Because()
    {
        await Erase();
        await Release();
        _legacy = await _manager.Release("store", "tenant", _schema, "subject", _stored);
    }

    [Fact]
    void should_report_only_protected_boundaries_in_ordinal_order() => _result.ErasedPaths.ShouldEqual<IEnumerable<string>>([
        "contacts[0].email", "contacts[1].email", "count", "dictionary.home", "flag", "nested[0][0]", "nullableNumber", "nullableObject", "nullableText", "object", "profile.name", "protectedLocation", "scalars[0]", "scalars[2]", "whole"]);
    [Fact] void should_preserve_legacy_release_content() => _result.Value.ToJsonString().ShouldEqual(_legacy.ToJsonString());
    [Fact] void should_restore_erased_arrays() => _result.Value["whole"].AsArray().Count.ShouldEqual(0);
    [Fact] void should_restore_erased_objects() => _result.Value["object"].AsObject().Count.ShouldEqual(0);
    [Fact] void should_restore_erased_numbers() => _result.Value["count"].GetValue<int>().ShouldEqual(0);
    [Fact] void should_restore_erased_booleans() => _result.Value["flag"].GetValue<bool>().ShouldBeFalse();
    [Fact] void should_preserve_nullable_number_restoration() => _result.Value["nullableNumber"].ShouldBeNull();
    [Fact] void should_preserve_nullable_string_restoration() => _result.Value["nullableText"].GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_preserve_nullable_container_restoration() => _result.Value["nullableObject"].AsObject().Count.ShouldEqual(0);
    [Fact] void should_leave_nulls_null() => _result.Value["absent"].ShouldBeNull();
    [Fact] void should_leave_unmarked_geospatial_values_alone() => _result.Value["location"].ToJsonString().ShouldEqual(_plaintext["location"].ToJsonString());
    [Fact] void should_keep_confidentiality_readable() => _result.Value["secret"].GetValue<string>().ShouldEqual("confidential");
    [Fact] void should_not_mutate_stored_ciphertext() => _stored.ToJsonString().ShouldEqual(_storedText);
    [Fact] async Task should_not_revive_the_subject_key() => (await _keys.HasFor("store", "tenant", "subject")).ShouldBeFalse();
}
