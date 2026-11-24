// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.Sinks.for_ChildKeyComparison.when_comparing_materializations;

public class and_keys_are_unequal_or_ambiguous : given.unequal_keys
{
    bool[] _results;

    void Because() => _results = _pairs.Select(pair => ChildKeyComparison.AreEqual(pair.Stored, pair.Lookup)).ToArray();

    [Fact] void should_reject_every_unequal_or_ambiguous_key() => _results.ShouldEachConformTo(result => !result);
}
