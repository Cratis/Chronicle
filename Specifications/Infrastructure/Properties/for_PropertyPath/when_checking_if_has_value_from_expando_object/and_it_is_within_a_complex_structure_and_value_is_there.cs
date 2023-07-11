// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Aksio.Cratis.Dynamic;

namespace Aksio.Cratis.Properties.for_PropertyPath.when_checking_if_has_value_from_expando_object;

public class and_it_is_within_a_complex_structure_and_value_is_there : Specification
{
    ExpandoObject input;
    PropertyPath property_path;
    bool result;
    ArrayIndexer first_array_indexer;
    ArrayIndexer second_array_indexer;

    void Establish()
    {
        input = new
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
        property_path = new("first_level.[second_level].third_level.[forth_level].fifth_level");
        first_array_indexer = new("first_level.[second_level]", "identifier", "first");
        second_array_indexer = new("first_level.[second_level].third_level.[forth_level]", "identifier", "second");
    }

    void Because() => result = property_path.HasValue(input, new ArrayIndexers(new[] { first_array_indexer, second_array_indexer }));

    [Fact] void should_not_have_it() => result.ShouldBeTrue();
}
