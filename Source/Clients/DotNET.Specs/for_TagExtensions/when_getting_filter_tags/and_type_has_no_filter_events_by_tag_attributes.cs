// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TagExtensions.when_getting_filter_tags;

public class and_type_has_no_filter_events_by_tag_attributes : Specification
{
    class TypeWithoutFilterTags;

    IEnumerable<string> _result;

    void Because() => _result = typeof(TypeWithoutFilterTags).GetFilterTags();

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
