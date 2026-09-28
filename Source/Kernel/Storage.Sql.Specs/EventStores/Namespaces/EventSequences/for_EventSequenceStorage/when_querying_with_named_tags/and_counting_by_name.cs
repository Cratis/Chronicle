// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_counting_by_name : given.a_storage_with_named_tags
{
    EventCount _nameOnly;
    EventCount _exact;
    EventCount _multipleValues;
    EventCount _multipleNames;
    EventCount _otherDimension;
    EventCount _wrongCase;
    EventCount _differentElements;

    async Task Because()
    {
        _nameOnly = await Count(new(new TagName("account")));
        _exact = await Count(new(new TagName("account"), ["a:b"]));
        _multipleValues = await Count(new(new TagName("account"), ["a:b", "b"]));
        _multipleNames = await _storage.GetCountMatching(new() { NamedTags = [new(new TagName("account"), ["a:b"]), new(new TagName("project"), ["99"])] });
        _otherDimension = await _storage.GetCountMatching(new() { EventSourceId = new EventSourceId("two"), NamedTags = [new(new TagName("account"), ["a:b"])] });
        _wrongCase = await Count(new(new TagName("Account")));
        _differentElements = await Count(new(new TagName("account"), ["42"]));
    }

    Task<EventCount> Count(NamedTagCriterion criterion) => _storage.GetCountMatching(new() { NamedTags = [criterion] });

    [Fact] void should_match_name_without_value() => _nameOnly.Value.ShouldEqual(2UL);
    [Fact] void should_match_exact_opaque_value() => _exact.Value.ShouldEqual(1UL);
    [Fact] void should_or_values() => _multipleValues.Value.ShouldEqual(2UL);
    [Fact] void should_or_criteria() => _multipleNames.Value.ShouldEqual(2UL);
    [Fact] void should_and_other_dimensions() => _otherDimension.Value.ShouldEqual(0UL);
    [Fact] void should_compare_names_ordinally() => _wrongCase.Value.ShouldEqual(0UL);
    [Fact] void should_not_pair_name_and_value_on_different_elements() => _differentElements.Value.ShouldEqual(0UL);
}
