// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.for_CategoryExtensions.when_getting_categories;

public class with_mixed_attributes : Specification
{
    [Category("Analytics", "Reporting")]
    [Category("Dashboard")]
    [Category("Security", "Compliance")]
    class TestObserver;

    IEnumerable<string> _result;

    void Because() => _result = typeof(TestObserver).GetCategories();

    [Fact] void should_return_all_categories() => _result.ShouldContainOnly(["Analytics", "Reporting", "Dashboard", "Security", "Compliance"]);
}
