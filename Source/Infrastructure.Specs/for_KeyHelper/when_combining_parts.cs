// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.for_KeyHelper;

public class when_combining_parts : Specification
{
    object[] _parts;
    string _result;

    void Establish() => _parts = ["First", "Second", "Third"];

    void Because() => _result = KeyHelper.Combine(_parts);

    [Fact] public void should_combine_parts_into_a_string_representation_of_a_key() => _result.ShouldEqual($"{_parts[0]}{KeyHelper.Separator}{_parts[1]}{KeyHelper.Separator}{_parts[2]}");
}
