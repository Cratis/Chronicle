// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.ReadModels.for_DecisionReads.when_admitting_for_stream;

public class and_all_properties_use_the_stream_key : given.a_decision_reader
{
    DecisionReadAdmission _result;

    void Establish()
    {
        _definition.From.First().Value.Key = "$eventContext(eventStreamId)";
        _definition.All.Key = "$eventContext(eventStreamId)";
        _definition.All.Properties["status"] = "$eventType";
    }

    void Because() => _result = _reader.AdmitForStream<Model>();

    [Fact] void should_admit_consistent_instance_keys() => _result.IsAdmitted.ShouldBeTrue();
}
