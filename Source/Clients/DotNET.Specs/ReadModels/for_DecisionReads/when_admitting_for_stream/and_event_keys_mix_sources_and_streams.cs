// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_admitting_for_stream;

public class and_event_keys_mix_sources_and_streams : given.a_decision_reader
{
    DecisionReadAdmission _result;

    void Establish() => _definition.RemovedWith[new() { Id = "removed", Generation = 1 }] = new() { Key = "$eventContext(eventStreamId)" };
    void Because() => _result = _reader.AdmitForStream<Model>();

    [Fact] void should_refuse_inconsistent_instance_keys() => _result.Reason.ShouldEqual(DecisionReadRefusalReason.NotEventSourceKeyed);
}
