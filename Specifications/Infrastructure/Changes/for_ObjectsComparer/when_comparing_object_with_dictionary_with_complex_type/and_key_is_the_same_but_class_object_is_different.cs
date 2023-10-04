// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Strings;

namespace Aksio.Cratis.Changes.for_ObjectComparer.when_comparing_object_with_dictionary_with_complex_type;

public class and_key_is_the_same_but_class_object_is_different : given.an_object_comparer
{
    class OtherType
    {
        public string Value { get; set; }
    }

    record TheType(IDictionary<string, OtherType> Dictionary);

    TheType left;
    TheType right;

    bool result;
    IEnumerable<PropertyDifference> differences;

    void Establish()
    {
        left = new(new Dictionary<string, OtherType>
        {
            { "Key", new()
                {
                    Value = "Value"
                }
            }
        });
        right = new(new Dictionary<string, OtherType>
        {
            { "Key", new()
                {
                    Value = "Something else"
                }
            }
        });
    }

    void Because() => result = comparer.Equals(left, right, out differences);

    [Fact] void should_not_be_equal() => result.ShouldBeFalse();
    [Fact] void should_only_have_one_property_difference() => differences.Count().ShouldEqual(1);
    [Fact] void should_have_dictionary_property_as_difference() => differences.First().PropertyPath.Path.ShouldEqual(nameof(TheType.Dictionary).ToCamelCase());
}
