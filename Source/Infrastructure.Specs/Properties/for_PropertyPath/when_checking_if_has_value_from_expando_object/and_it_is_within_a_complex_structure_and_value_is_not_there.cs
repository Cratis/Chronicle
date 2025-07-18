// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.Properties.for_PropertyPath.when_checking_if_has_value_from_expando_object;

public class and_it_is_within_a_complex_structure_and_value_is_not_there : Specification
{
    ExpandoObject _input;
    PropertyPath _propertyPath;
    bool _result;
    ArrayIndexer _firstArrayIndexer;
    ArrayIndexer _secondArrayIndexer;

    void Establish()
    {
        _input = new();
        _propertyPath = new("first_level.[second_level].third_level.[forth_level].fifth_level");
        _firstArrayIndexer = new("first_level.[second_level]", "identifier", "first");
        _secondArrayIndexer = new("first_level.[second_level].third_level.[forth_level]", "identifier", "second");
    }

    void Because() => _result = _propertyPath.HasValue(_input, new ArrayIndexers([_firstArrayIndexer, _secondArrayIndexer]));

    [Fact] void should_not_have_it() => _result.ShouldBeFalse();
}
