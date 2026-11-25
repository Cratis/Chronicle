// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_KeyValueEquality.when_comparing_composite_keys;

public class and_equivalent_materializations_are_reversed : given.equivalent_keys
{
    bool[] _results;

    void Because() => _results = _pairs.Select(pair => KeyValueEquality.AreEqual(pair.Lookup, pair.Stored)).ToArray();

    [Fact] void should_match_all_structural_materializations() => _results.ShouldEachConformTo(result => result);
}
