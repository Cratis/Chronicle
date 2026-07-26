// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class when_adding_a_property_with_an_enumerable_type_and_a_name_that_is_not_a_single_word : Specification
{
    const string Left = "left";
    const string Right = "two words";

    PropertyPath _result;

    void Because() => _result = new PropertyPath(Left).AddProperty(Right, typeof(List<string>));

    [Fact] void should_keep_the_bracket_that_does_not_form_an_array_indexer() => _result.Path.ShouldEqual($"{Left}.[{Right}]]");
    [Fact] void should_have_last_segment_be_a_property_name() => _result.LastSegment.ShouldBeOfExactType<PropertyName>();
}
