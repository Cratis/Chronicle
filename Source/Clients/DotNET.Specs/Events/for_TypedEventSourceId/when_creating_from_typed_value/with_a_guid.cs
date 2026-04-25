// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Events.for_TypedEventSourceId.when_creating_from_typed_value;

public class with_a_guid : Specification
{
    static readonly Guid _input = Guid.Parse("b4e2b6a0-1b3e-4e6a-8c4d-2f7e9b1a3c5d");
    EventSourceId<Guid> _result;

    void Because() => _result = _input;

    [Fact] void should_have_the_guid_string_representation_as_value() => ((EventSourceId)_result).Value.ShouldEqual(_input.ToString());
    [Fact] void should_have_the_guid_as_typed_value() => _result.TypedValue.ShouldEqual(_input);
}
