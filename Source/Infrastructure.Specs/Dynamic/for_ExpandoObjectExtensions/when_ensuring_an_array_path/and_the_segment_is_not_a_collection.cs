// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Dynamic.for_ExpandoObjectExtensions.when_ensuring_an_array_path;

public class and_the_segment_is_not_a_collection : Specification
{
    ExpandoObject _state;
    Exception _exception;

    void Establish()
    {
        _state = new ExpandoObject();
        ((IDictionary<string, object?>)_state)["children"] = 42;
    }

    void Because() => _exception = Catch.Exception(() => _state.EnsurePath(
        new PropertyPath("[children].topic"),
        new ArrayIndexers([new ArrayIndexer("[children]", "id", "A")])));

    [Fact] void should_throw_segment_value_is_not_collection() => _exception.ShouldBeOfExactType<SegmentValueIsNotCollection>();
    [Fact] void should_have_message() => _exception.Message.ShouldEqual("Segment 'children' in '[children].topic' is not a collection of ExpandoObject");
}
