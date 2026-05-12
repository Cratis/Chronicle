// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_TagExtensions.when_getting_filter_tags;

public class and_type_has_single_filter_events_by_tag_attribute : Specification
{
    [FilterEventsByTag("important")]
    class TypeWithOneFilterTag;

    IEnumerable<string> _result;

    void Because() => _result = typeof(TypeWithOneFilterTag).GetFilterTags();

    [Fact] void should_return_one_tag() => _result.Count().ShouldEqual(1);
    [Fact] void should_contain_the_tag() => _result.ShouldContain("important");
}
