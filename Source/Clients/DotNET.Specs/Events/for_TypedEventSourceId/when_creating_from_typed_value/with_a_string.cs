// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_creating_from_typed_value;

public class with_a_string : Specification
{
    const string _input = "some-id";
    EventSourceId<string> _result;

    void Because() => _result = new EventSourceId<string>(_input);

    [Fact] void should_have_the_string_as_value() => _result.Value.ShouldEqual(_input);
    [Fact] void should_have_the_string_as_typed_value() => _result.TypedValue.ShouldEqual(_input);
}
