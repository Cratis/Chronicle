// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_CategoryExtensions.when_getting_categories;

public class without_category_attributes : Specification
{
    class TestObserver;

    IEnumerable<string> _result;

    void Because() => _result = typeof(TestObserver).GetCategories();

    [Fact] void should_return_empty_collection() => _result.ShouldBeEmpty();
}
