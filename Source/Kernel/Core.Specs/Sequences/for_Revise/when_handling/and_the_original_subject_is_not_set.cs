// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Sequences.for_Revise.when_handling;

public class and_the_original_subject_is_not_set : and_the_request_contains_nested_pii
{
    protected override Subject? OriginalSubject => Subject.NotSet;

    [Fact] void should_release_with_the_event_source_fallback() => _released["profile"]!["name"]!.GetValue<string>().ShouldEqual("revised name");
    [Fact] void should_erase_with_the_event_source_fallback() => _erased["profile"]!["name"]!.GetValue<string>().ShouldEqual(string.Empty);
    [Fact] void should_reject_new_pii_for_the_erased_event_source() => _errorAfterErasure.ShouldBeOfExactType<SchemaMetadataActionFailed>();
}
