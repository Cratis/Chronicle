// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Dynamic;

namespace Cratis.Chronicle.Objects.for_ObjectExtensions.when_comparing_expando_objects;

public class and_one_identity_is_a_subset : Specification
{
    bool[] _results;
    (object Left, object Right)[] _pairs;

    void Establish()
    {
        var subset = new { tenant = "tenant" }.AsExpandoObject();
        var full = new { tenant = "tenant", number = 7 }.AsExpandoObject();
        var nestedSubset = new { key = subset }.AsExpandoObject();
        var nestedFull = new { key = full }.AsExpandoObject();
        _pairs = [(subset, full), (full, subset), (nestedSubset, nestedFull), (nestedFull, nestedSubset)];
    }

    void Because() => _results = _pairs.Select(pair => pair.Left.IsEqualTo(pair.Right)).ToArray();

    [Fact] void should_reject_subsets_in_both_directions_at_every_depth() => _results.ShouldEachConformTo(result => !result);
}
