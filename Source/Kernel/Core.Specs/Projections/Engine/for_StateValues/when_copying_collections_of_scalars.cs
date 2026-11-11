// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Projections.Engine.for_StateValues;

public class when_copying_collections_of_scalars : Specification
{
    int[] _numbers;
    List<string> _strings;
    object _numbersResult;
    object _stringsResult;

    void Establish()
    {
        _numbers = [1, 2, 3];
        _strings = ["a", "b"];
    }

    void Because()
    {
        _numbersResult = StateValues.Copy(_numbers);
        _stringsResult = StateValues.Copy(_strings);
    }

    [Fact] void should_copy_an_integer_array_into_a_list() => _numbersResult.ShouldBeOfExactType<List<int>>();
    [Fact] void should_keep_the_numbers() => ((List<int>)_numbersResult).ShouldContainOnly(_numbers);
    [Fact] void should_return_another_string_list() => _stringsResult.ShouldNotBeSame(_strings);
    [Fact] void should_keep_the_string_list_type() => _stringsResult.ShouldBeOfExactType<List<string>>();
    [Fact] void should_keep_the_strings() => ((List<string>)_stringsResult).ShouldContainOnly(_strings);
}
