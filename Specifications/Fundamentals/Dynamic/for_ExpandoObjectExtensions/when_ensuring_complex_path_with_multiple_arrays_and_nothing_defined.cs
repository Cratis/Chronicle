// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_complex_path_with_multiple_arrays_and_nothing_defined : Specification
{
    ExpandoObject initial;
    PropertyPath property_path;
    ExpandoObject result;
    dynamic initial_as_dynamic;
    ArrayIndexer first_array_indexer;
    ArrayIndexer second_array_indexer;

    void Establish()
    {
        initial_as_dynamic = initial = new();
        property_path = new("first_level.[second_level].third_level.[forth_level].fifth_level");
        first_array_indexer = new("first_level.[second_level]", "identifier", "first");
        second_array_indexer = new("first_level.[second_level].third_level.[forth_level]", "identifier", "second");
    }

    void Because() => result = initial.EnsurePath(property_path, new ArrayIndexers(new[] { first_array_indexer, second_array_indexer }));

    [Fact] void should_return_a_new_object() => result.ShouldNotBeNull();
    [Fact] void should_add_all_levels_and_include_returning_object() => ((ExpandoObject)initial_as_dynamic.first_level.second_level[0].third_level.forth_level[0]).ShouldEqual(result);
}
