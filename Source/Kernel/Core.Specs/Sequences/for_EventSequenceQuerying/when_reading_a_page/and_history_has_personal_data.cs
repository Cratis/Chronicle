// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.when_reading_a_page;

public class and_history_has_personal_data : given.a_page_with_protected_history
{
    async Task Because() => await ReadPage();

    [Fact] void should_release_all_generation_schemas_resolved_from_storage() => _result[0].GenerationalContent.ShouldEachConformTo(item => Value(item.Value, item.Key) == "released");
    [Fact] void should_release_original_content() => Value(_result[0].OriginalContent, 1).ShouldEqual("released");
    [Fact] void should_release_revision_content() => Value(_result[0].Revisions.Single().Content, 2).ShouldEqual("released");
}
