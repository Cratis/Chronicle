// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TagExtensions.when_getting_filter_tags;

public class and_type_has_multiple_filter_events_by_tag_attributes : Specification
{
    [FilterEventsByTag("important")]
    [FilterEventsByTag("priority")]
    class TypeWithMultipleFilterTags;

    IEnumerable<string> _result;

    void Because() => _result = typeof(TypeWithMultipleFilterTags).GetFilterTags();

    [Fact] void should_return_all_tags() => _result.Count().ShouldEqual(2);
    [Fact] void should_contain_first_tag() => _result.ShouldContain("important");
    [Fact] void should_contain_second_tag() => _result.ShouldContain("priority");
}
