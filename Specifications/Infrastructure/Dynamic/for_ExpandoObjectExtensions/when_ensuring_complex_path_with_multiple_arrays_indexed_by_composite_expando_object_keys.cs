// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_complex_path_with_multiple_arrays_indexed_by_composite_expando_object_keys : Specification
{
    ExpandoObject initial;
    PropertyPath property_path;
    ExpandoObject result;
    ArrayIndexer first_array_indexer;
    ArrayIndexer second_array_indexer;
    ExpandoObject first_key;
    ExpandoObject second_key;

    void Establish()
    {
        first_key = new ExpandoObject();

        ((dynamic)first_key).FirstCompositeProperty = "FirstKey FirstProperty";
        ((dynamic)first_key).SecondCompositeProperty = "FirstKey SecondProperty";

        second_key = new ExpandoObject();
        ((dynamic)second_key).FirstCompositeProperty = "SecondKey FirstProperty";
        ((dynamic)second_key).SecondCompositeProperty = "SecondKey SecondProperty";

        initial = new
        {
            first_level = new
            {
                second_level = new[]
                {
                    new
                    {
                        identifier = new
                        {
                            ((dynamic)first_key).FirstCompositeProperty,
                            ((dynamic)first_key).SecondCompositeProperty
                        },
                        third_level = new
                        {
                            forth_level = new[]
                            {
                                new
                                {
                                    identifier = new
                                    {
                                        ((dynamic)second_key).FirstCompositeProperty,
                                        ((dynamic)second_key).SecondCompositeProperty
                                    },
                                    fifth_level = 42
                                }
                            }
                        }
                    }
                }
            }
        }.AsExpandoObject();
        property_path = new("first_level.[second_level].third_level.[forth_level].fifth_level");
        first_array_indexer = new("first_level.[second_level]", "identifier", first_key);
        second_array_indexer = new("first_level.[second_level].third_level.[forth_level]", "identifier", second_key);
    }

    void Because() => result = initial.EnsurePath(property_path, new ArrayIndexers(new[] { first_array_indexer, second_array_indexer }));

    [Fact] void should_return_existing_object() => ((int)((dynamic)result).fifth_level).ShouldEqual(42);
}
