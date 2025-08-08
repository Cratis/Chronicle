// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Changes.for_ObjectComparer.when_comparing_object_with_dictionary_with_complex_type;

public class and_key_is_the_same_but_class_object_is_different : given.an_object_comparer
{
    class OtherType
    {
        public string Value { get; set; }
    }

    record TheType(IDictionary<string, OtherType> Dictionary);

    TheType _left;
    TheType _right;

    bool _result;
    IEnumerable<PropertyDifference> _differences;

    void Establish()
    {
        _left = new(new Dictionary<string, OtherType>
        {
            { "Key", new()
                {
                    Value = "Value"
                }
            }
        });
        _right = new(new Dictionary<string, OtherType>
        {
            { "Key", new()
                {
                    Value = "Something else"
                }
            }
        });
    }

    void Because() => _result = comparer.Compare(_left, _right, out _differences);

    [Fact] void should_not_be_equal() => _result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => _differences.Count().ShouldEqual(1);
    [Fact] void should_have_dictionary_property_as_difference() => _differences.First().PropertyPath.Path.ShouldEqual(nameof(TheType.Dictionary));
}
