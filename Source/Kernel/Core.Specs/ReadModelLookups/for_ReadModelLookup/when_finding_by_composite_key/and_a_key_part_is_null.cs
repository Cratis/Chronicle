// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_a_key_part_is_null : given.a_composite_key_read
{
    void Establish() => _parts["first"] = "null";
    Task Because() => Read();
    [Fact] void should_map_the_store_outcome() => _result.Outcome.ShouldEqual(ReadModelLookupOutcome.Found);
    [Fact] void should_pass_the_null_part_to_the_store() => ((IDictionary<string, object?>)(ExpandoObject)_key.Value)["first"].ShouldBeNull();
}
