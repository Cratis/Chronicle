// Copyright (c) Aksio Insurtech. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Aksio.Cratis.Properties;

namespace Aksio.Cratis.Objects.for_ObjectExtensions;

public class when_ensuring_complex_path_with_multiple_arrays_and_nothing_defined : Specification
{
    TopLevel initial;
    PropertyPath property_path;
    ForthLevel result;
    ArrayIndexer first_array_indexer;
    ArrayIndexer second_array_indexer;

    void Establish()
    {
        initial = new(null);
        property_path = new($"{nameof(TopLevel.FirstLevel)}.[{nameof(FirstLevel.SecondLevel)}].{nameof(SecondLevel.ThirdLevel)}.[{nameof(ThirdLevel.ForthLevel)}].{nameof(ForthLevel.SomeProperty)}");
        first_array_indexer = new($"{nameof(TopLevel.FirstLevel)}.[{nameof(FirstLevel.SecondLevel)}]", "identifier", "first");
        second_array_indexer = new($"{nameof(TopLevel.FirstLevel)}.[{nameof(FirstLevel.SecondLevel)}].{nameof(SecondLevel.ThirdLevel)}.[{nameof(ThirdLevel.ForthLevel)}]", "identifier", "second");
    }

    void Because() => result = initial.EnsurePath(property_path, new ArrayIndexers(new[] { first_array_indexer, second_array_indexer })) as ForthLevel;

    [Fact] void should_return_a_new_object() => result.ShouldNotBeNull();
    [Fact] void should_add_all_levels_and_include_returning_object() => initial.FirstLevel.SecondLevel.First().ThirdLevel.ForthLevel.First().ShouldEqual(result);
}
