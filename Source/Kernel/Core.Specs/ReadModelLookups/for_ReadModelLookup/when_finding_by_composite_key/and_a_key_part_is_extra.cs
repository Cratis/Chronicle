// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_a_key_part_is_extra : given.a_composite_key_read
{
    void Establish() => _parts["extra"] = "1";
    Task Because() => Read();
    [Fact] void should_report_invalid_key() => _result.Outcome.ShouldEqual(ReadModelLookupOutcome.InvalidKey);
    [Fact] void should_return_json_null() => _result.ReadModel.ShouldEqual("null");
    [Fact] Task should_not_look_up_the_instance() => DidNotLookUp();
}
