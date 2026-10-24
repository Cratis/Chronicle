// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Sinks;

namespace Cratis.Chronicle.ReadModelLookups.for_ReadModelLookup.when_finding_by_composite_key;

public class and_the_read_model_is_passive : given.a_composite_key_read
{
    void Establish() => _definition = _definition with { Sink = SinkDefinition.None };
    Task Because() => Read();
    [Fact] void should_report_not_supported() => _result.Outcome.ShouldEqual(ReadModelLookupOutcome.NotSupported);
    [Fact] Task should_not_look_up_the_instance() => DidNotLookUp();
}
