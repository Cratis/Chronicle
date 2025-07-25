// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Properties.for_PropertyPath;

public class and_it_is_within_a_complex_structure_with_multiple_arrays : Specification
{
    ExpandoObject _input;
    PropertyPath _propertyPath;
    object _result;
    ArrayIndexer _firstArrayIndexer;
    ArrayIndexer _secondArrayIndexer;

    void Establish()
    {
        _input = new
        {
            first_level = new
            {
                second_level = new[]
                {
                    new
                    {
                        identifier = "first",
                        third_level = new
                        {
                            forth_level = new[]
                            {
                                new
                                {
                                    identifier = "second",
                                    fifth_level = 42
                                }
                            }
                        }
                    }
                }
            }
        }.AsExpandoObject();
        _propertyPath = new("first_level.[second_level].third_level.[forth_level].fifth_level");
        _firstArrayIndexer = new("first_level.[second_level]", "identifier", "first");
        _secondArrayIndexer = new("first_level.[second_level].third_level.[forth_level]", "identifier", "second");
    }

    void Because() => _result = _propertyPath.GetValue(_input, new ArrayIndexers([_firstArrayIndexer, _secondArrayIndexer]));

    [Fact] void should_return_value() => _result.ShouldEqual(42);
}
