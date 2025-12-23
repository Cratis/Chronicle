// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_CategoryExtensions.when_getting_categories;

public class with_multiple_attributes_with_single_category : Specification
{
    [Category("Analytics")]
    [Category("Reporting")]
    class TestObserver;

    IEnumerable<string> _result;

    void Because() => _result = typeof(TestObserver).GetCategories();

    [Fact] void should_return_all_categories() => _result.ShouldContainOnly(["Analytics", "Reporting"]);
}
