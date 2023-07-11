// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Dynamic.for_ExpandoObjectExtensions;

public class when_ensuring_complex_path_with_multiple_arrays_with_property_path_not_set : Specification
{
    ExpandoObject initial;
    PropertyPath property_path;
    ExpandoObject result;
    ArrayIndexer first_array_indexer;
    ArrayIndexer second_array_indexer;

    void Establish()
    {
        initial = new
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
                                    fifth_level = 42
                                },
                                new
                                {
                                    fifth_level = 43
                                }
                            }
                        }
                    }
                }
            }
        }.AsExpandoObject();
        property_path = new("first_level.[second_level].third_level.[forth_level].fifth_level");
        first_array_indexer = new("first_level.[second_level]", "identifier", "first");
        second_array_indexer = new("first_level.[second_level].third_level.[forth_level]", PropertyPath.NotSet, 1);
    }

    void Because() => result = initial.EnsurePath(property_path, new ArrayIndexers(new[] { first_array_indexer, second_array_indexer }));

    [Fact] void should_return_existing_object() => ((int)((dynamic)result).fifth_level).ShouldEqual(43);
}
