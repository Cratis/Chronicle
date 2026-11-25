// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildKeyComparison.when_comparing_materializations;

public class and_equivalent_keys_are_reversed : given.equivalent_keys
{
    bool[] _results;

    void Because() => _results = _pairs.Select(pair => ChildKeyComparison.AreEqual(pair.Lookup, pair.Stored)).ToArray();

    [Fact] void should_match_every_representation() => _results.ShouldEachConformTo(result => result);
}
