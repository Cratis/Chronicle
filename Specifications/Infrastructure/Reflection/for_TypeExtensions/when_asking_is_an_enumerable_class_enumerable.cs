// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Reflection.for_TypeExtensions;

public class when_asking_is_an_enumerable_class_enumerable : Specification
{
    bool IEnumerable_is_enumerable;
    bool ICollection_is_enumerable;
    bool array_of_object_is_enumerable;
    bool IEnumerable_of_string_is_enumerable;
    bool IEnumerable_of_object_is_enumerable;
    bool IEnumerable_of_IEnumerable_of_object_is_enumerable;
    bool IEnumerable_of_complex_type_is_enumerable;
    bool MyEnumerable_is_enumerable;
    bool object_is_enumerable;
    bool string_is_enumerable;
    bool char_is_enumerable;

    void Because()
    {
        IEnumerable_is_enumerable = typeof(System.Collections.IEnumerable).IsEnumerable();
        ICollection_is_enumerable = typeof(System.Collections.ICollection).IsEnumerable();
        array_of_object_is_enumerable = typeof(object[]).IsEnumerable();
        IEnumerable_of_object_is_enumerable = typeof(IEnumerable<object>).IsEnumerable();
        IEnumerable_of_string_is_enumerable = typeof(IEnumerable<string>).IsEnumerable();
        IEnumerable_of_IEnumerable_of_object_is_enumerable = typeof(IEnumerable<IEnumerable<object>>).IsEnumerable();
        IEnumerable_of_complex_type_is_enumerable = typeof(IEnumerable<ComplexType>).IsEnumerable();
        MyEnumerable_is_enumerable = typeof(MyEnumerable).IsEnumerable();

        object_is_enumerable = typeof(object).IsEnumerable();
        string_is_enumerable = typeof(string).IsEnumerable();
        char_is_enumerable = typeof(char).IsEnumerable();
    }

    [Fact] void should_consider_IEnumerable_as_enumerable() => IEnumerable_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_ICollection_as_enumerable() => ICollection_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_array_of_object_as_enumerable() => array_of_object_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_IEnumerable_of_string_as_enumerable() => IEnumerable_of_string_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_IEnumerable_of_object_as_enumerable() => IEnumerable_of_object_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_IEnumerable_of_IEnumerable_of_object_is_enumerable() => IEnumerable_of_IEnumerable_of_object_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_IEnumerable_of_complex_type_as_enumerable() => IEnumerable_of_complex_type_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_MyEnumerable_as_enumerable() => MyEnumerable_is_enumerable.ShouldBeTrue();
    [Fact] void should_consider_object_as_not_enumerable() => object_is_enumerable.ShouldBeFalse();
    [Fact] void should_consider_string_as_not_enumerable() => string_is_enumerable.ShouldBeFalse();
    [Fact] void should_consider_char_as_not_enumerable() => char_is_enumerable.ShouldBeFalse();
}
