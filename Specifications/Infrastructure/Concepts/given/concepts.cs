// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Concepts.given;

public class concepts : Specification
{
    protected StringConcept first_string;
    protected StringConcept second_string;
    protected StringConcept same_value_as_second_string;
    protected StringConcept string_is_empty;
    protected IntConcept value_as_an_int;
    protected LongConcept value_as_a_long;
    protected InheritingFromLongConcept value_as_a_long_inherited;
    protected InheritingFromLongConcept empty_long_value;

    void Establish()
    {
        first_string = "first";
        second_string = "second";
        same_value_as_second_string = "second";
        string_is_empty = string.Empty;

        value_as_a_long = 1;
        value_as_an_int = 1;
        value_as_a_long_inherited = 1;
        empty_long_value = 0;
    }
}
