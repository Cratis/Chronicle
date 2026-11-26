// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Dynamic.for_KeyValueEquality.when_comparing_composite_keys;

public class and_keys_are_unequal_or_ambiguous : given.unequal_keys
{
    bool[] _results;

    void Because() => _results = _pairs.Select(pair => KeyValueEquality.AreEqual(pair.Stored, pair.Lookup)).ToArray();

    [Fact] void should_reject_every_unequal_or_ambiguous_key() => _results.ShouldEachConformTo(result => !result);
}
