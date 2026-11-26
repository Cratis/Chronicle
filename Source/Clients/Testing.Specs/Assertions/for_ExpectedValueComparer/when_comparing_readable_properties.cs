// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Testing.Assertions.for_ExpectedValueComparer.given;

namespace Cratis.Chronicle.Testing.Assertions.for_ExpectedValueComparer;

public class when_comparing_readable_properties : Specification
{
    ValueDifference? _matching;
    ValueDifference? _missing;
    ValueDifference? _case;

    void Because()
    {
        _matching = ExpectedValueComparer.Compare(new { Name = "Ada" }, new IndexedShape(), ValueComparison.Exact);
        _missing = ExpectedValueComparer.Compare(new { Name = "Ada", Missing = (object?)null }, new IndexedShape(), ValueComparison.Partial);
        _case = ExpectedValueComparer.Compare(new { name = "Ada" }, new IndexedShape(), ValueComparison.Partial);
    }

    [Fact] void should_ignore_static_properties_and_indexers() => _matching.ShouldBeNull();
    [Fact] void should_distinguish_a_missing_property_from_a_null_value() => _missing!.Path.ShouldEqual("$.Missing");
    [Fact] void should_compare_property_names_ordinally() => _case!.Path.ShouldEqual("$.name");
}
