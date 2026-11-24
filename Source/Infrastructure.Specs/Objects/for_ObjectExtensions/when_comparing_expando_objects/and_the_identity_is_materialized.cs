// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Objects.for_ObjectExtensions.when_comparing_expando_objects;

public class and_the_identity_is_materialized : Specification
{
    bool[] _results;
    (object Left, object Right)[] _pairs;

    void Establish()
    {
        var stored = new { tenant = "tenant", number = 7L, id = "AD037C15-1354-4CD7-A069-F24AA1706109" }.AsExpandoObject();
        var lookup = new { Tenant = "tenant", Number = 7, Id = Guid.Parse("ad037c15-1354-4cd7-a069-f24aa1706109") };
        _pairs = [(stored, lookup), (lookup, stored), (stored, lookup.AsExpandoObject())];
    }

    void Because() => _results = _pairs.Select(pair => pair.Left.IsEqualTo(pair.Right)).ToArray();

    [Fact] void should_match_equivalent_structured_leaves_in_both_directions() => _results.ShouldEachConformTo(result => result);
}
